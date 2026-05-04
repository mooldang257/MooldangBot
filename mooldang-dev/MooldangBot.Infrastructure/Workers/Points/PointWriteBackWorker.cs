using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Point.Interfaces;

namespace MooldangBot.Infrastructure.Workers.Points;

/// <summary>
/// [v7.2] 포인트 Write-Back 워커 (Zero-Loss): 
/// 2-Phase Commit(Snapshot) 패턴을 사용하여 Redis 변동분을 안전하게 DB로 플러시합니다.
/// </summary>
public class PointWriteBackWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPointCacheService _pointCache;
    private readonly ILogger<PointWriteBackWorker> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(10);

    public PointWriteBackWorker(
        IServiceProvider serviceProvider, 
        IPointCacheService pointCache,
        ILogger<PointWriteBackWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _pointCache = pointCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var IsDisabled = Environment.GetEnvironmentVariable("DISABLE_POINT_WRITEBACK") == "true";
        if (IsDisabled)
        {
            _logger.LogWarning("🚫 [WriteBack] 환경 변수 설정에 의해 가동이 중단되었습니다.");
            return;
        }
 
        _logger.LogInformation("🚀 [WriteBack] 포인트 동기화 워커가 기동되었습니다. (장애 복구 모드 활성화)");

        // 0. [복구 단계]: 이전 기동 시 처리하지 못한 스냅샷이 있다면 먼저 처리 (Idempotency 보장)
        try 
        {
            var OrphanedSnapshots = await _pointCache.GetOrphanedSnapshotsAsync();
            foreach (var SnapshotId in OrphanedSnapshots)
            {
                _logger.LogWarning("🩹 [WriteBack] 미완료 스냅샷 발견: {SnapshotId}. 복구를 시작합니다.", SnapshotId);
                await ProcessSnapshotWithRetryAsync(SnapshotId, ct);
            }
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, "⚠️ [WriteBack] 장애 복구 도중 오류 발생 (무시하고 정기 사이클 진입)");
        }

        // 1. [정기 사이클]: Snapshot -> Sync -> Commit 루프
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var SnapshotId = await _pointCache.CreateSyncSnapshotAsync();
                
                if (!string.IsNullOrEmpty(SnapshotId))
                {
                    await ProcessSnapshotWithRetryAsync(SnapshotId, ct);
                }
            }
            catch (Exception Ex)
            {
                _logger.LogError(Ex, "🚨 [WriteBack] 동기화 사이클 중 치명적 예외 발생");
            }

            await Task.Delay(_syncInterval, ct);
        }
    }

    private async Task ProcessSnapshotWithRetryAsync(string SnapshotId, CancellationToken ct)
    {
        try 
        {
            // 스냅샷 데이터 조회
            var Increments = await _pointCache.GetSnapshotDataAsync(SnapshotId);
            if (Increments.Count > 0)
            {
                _logger.LogInformation("🔄 [WriteBack] {Count}건의 스냅샷({SnapshotId}) 동기화를 시도합니다.", Increments.Count, SnapshotId);
                
                using var Scope = _serviceProvider.CreateScope();
                await SyncToDatabaseAsync(Increments, Scope, ct);
                
                // DB 커밋 성공 시에만 Redis 스냅샷 제거 (Commit)
                await _pointCache.RemoveSnapshotAsync(SnapshotId);
                _logger.LogInformation("✅ [WriteBack] 스냅샷 {SnapshotId} 동기화 및 정리 완료!", SnapshotId);
            }
            else 
            {
                await _pointCache.RemoveSnapshotAsync(SnapshotId);
            }
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, "❌ [WriteBack] 스냅샷 {SnapshotId} 처리 실패. 다음 사이클에 재시도합니다.", SnapshotId);
        }
    }

    private async Task SyncToDatabaseAsync(IDictionary<string, PointVariant> Data, IServiceScope Scope, CancellationToken ct)
    {
        var Db = Scope.ServiceProvider.GetRequiredService<IPointDbContext>();
        var Connection = Db.Database.GetDbConnection();
        
        if (Connection.State != ConnectionState.Open) await Connection.OpenAsync(ct);
 
        // 데이터 전처리 (UID 파싱 및 해시 생성)
        var Items = Data.Select(kvp => {
            var Parts = kvp.Key.Split(':');
            var StreamerUid = Parts[0];
            var RawViewerUid = Parts.Length > 1 ? Parts[1] : "unknown";
            
            // [v6.2.3] UID 정규화: 대소문자 무관 및 공백 제거 처리
            var ViewerUid = RawViewerUid.Trim().ToLowerInvariant();
            
            return new {
                StreamerUid = StreamerUid, 
                ViewerUid = ViewerUid,
                Hash = Sha256Hasher.ComputeHash(ViewerUid),
                Nickname = kvp.Value.Nickname,
                Amount = kvp.Value.Amount
            };
        }).Where(x => x.StreamerUid != "unknown" && x.ViewerUid != "unknown").ToList();
 
        if (Items.Count == 0) return;

        const int ChunkSize = 500;
        for (int i = 0; i < Items.Count; i += ChunkSize)
        {
            var Chunk = Items.Skip(i).Take(ChunkSize).ToList();
            using var Transaction = await Connection.BeginTransactionAsync(ct);
            try
            {
                // Step 1: Global Viewers Bulk Upsert
                const string globalSql = @"
                    INSERT INTO CoreGlobalViewers (ViewerUid, ViewerUidHash, Nickname, IsDeleted, CreatedAt, UpdatedAt)
                    VALUES (@ViewerUid, @Hash, @Nickname, 0, NOW(), NOW())
                    ON DUPLICATE KEY UPDATE Nickname = VALUES(Nickname), UpdatedAt = NOW();";
                await Connection.ExecuteAsync(globalSql, Chunk, Transaction);

                // Step 2: Relations Bulk Upsert
                const string relationUpsertSql = @"
                    INSERT INTO CoreViewerRelations (StreamerProfileId, GlobalViewerId, IsActive, IsDeleted, AttendanceCount, ConsecutiveAttendanceCount, FirstVisitAt, LastChatAt, CreatedAt, UpdatedAt)
                    SELECT s.Id, g.Id, 1, 0, 0, 0, NOW(), NOW(), NOW(), NOW()
                    FROM CoreStreamerProfiles s
                    JOIN CoreGlobalViewers g ON g.ViewerUidHash = @Hash
                    WHERE s.ChzzkUid = @StreamerUid
                    ON DUPLICATE KEY UPDATE LastChatAt = NOW(), UpdatedAt = NOW();";
                await Connection.ExecuteAsync(relationUpsertSql, Chunk, Transaction);

                // Step 3: Points Bulk Upsert
                const string pointUpsertSql = @"
                    INSERT INTO FuncViewerPoints (StreamerProfileId, GlobalViewerId, Points, CreatedAt, UpdatedAt)
                    SELECT s.Id, g.Id, @Amount, NOW(), NOW()
                    FROM CoreStreamerProfiles s
                    JOIN CoreGlobalViewers g ON g.ViewerUidHash = @Hash
                    WHERE s.ChzzkUid = @StreamerUid
                    ON DUPLICATE KEY UPDATE Points = Points + VALUES(Points), UpdatedAt = NOW();";
                await Connection.ExecuteAsync(pointUpsertSql, Chunk, Transaction);
 
                await Transaction.CommitAsync(ct);
            }
            catch (Exception Ex)
            {
                await Transaction.RollbackAsync(ct);
                _logger.LogError(Ex, "[WriteBack] DB 처리 중 오류 발생 (Rollback): {Msg}", Ex.Message);
                throw; // 상위로 던져서 스냅샷이 유지되도록 함
            }
        }
    }
}