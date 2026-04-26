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
        var isDisabled = Environment.GetEnvironmentVariable("DISABLE_POINT_WRITEBACK") == "true";
        if (isDisabled)
        {
            _logger.LogWarning("🚫 [WriteBack] 환경 변수 설정에 의해 가동이 중단되었습니다.");
            return;
        }

        _logger.LogInformation("🚀 [WriteBack] 포인트 동기화 워커가 기동되었습니다. (장애 복구 모드 활성화)");

        // 0. [복구 단계]: 이전 기동 시 처리하지 못한 스냅샷이 있다면 먼저 처리 (Idempotency 보장)
        try 
        {
            var orphanedSnapshots = await _pointCache.GetOrphanedSnapshotsAsync();
            foreach (var snapshotId in orphanedSnapshots)
            {
                _logger.LogWarning("🩹 [WriteBack] 미완료 스냅샷 발견: {SnapshotId}. 복구를 시작합니다.", snapshotId);
                await ProcessSnapshotWithRetryAsync(snapshotId, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "⚠️ [WriteBack] 장애 복구 도중 오류 발생 (무시하고 정기 사이클 진입)");
        }

        // 1. [정기 사이클]: Snapshot -> Sync -> Commit 루프
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var snapshotId = await _pointCache.CreateSyncSnapshotAsync();
                
                if (!string.IsNullOrEmpty(snapshotId))
                {
                    await ProcessSnapshotWithRetryAsync(snapshotId, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 [WriteBack] 동기화 사이클 중 치명적 예외 발생");
            }

            await Task.Delay(_syncInterval, ct);
        }
    }

    private async Task ProcessSnapshotWithRetryAsync(string snapshotId, CancellationToken ct)
    {
        try 
        {
            // 스냅샷 데이터 조회
            var increments = await _pointCache.GetSnapshotDataAsync(snapshotId);
            if (increments.Count > 0)
            {
                _logger.LogInformation("🔄 [WriteBack] {Count}건의 스냅샷({SnapshotId}) 동기화를 시도합니다.", increments.Count, snapshotId);
                
                using var scope = _serviceProvider.CreateScope();
                await SyncToDatabaseAsync(increments, scope, ct);
                
                // DB 커밋 성공 시에만 Redis 스냅샷 제거 (Commit)
                await _pointCache.RemoveSnapshotAsync(snapshotId);
                _logger.LogInformation("✅ [WriteBack] 스냅샷 {SnapshotId} 동기화 및 정리 완료!", snapshotId);
            }
            else 
            {
                await _pointCache.RemoveSnapshotAsync(snapshotId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [WriteBack] 스냅샷 {SnapshotId} 처리 실패. 다음 사이클에 재시도합니다.", snapshotId);
        }
    }

    private async Task SyncToDatabaseAsync(IDictionary<string, PointVariant> data, IServiceScope scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<IPointDbContext>();
        var connection = db.Database.GetDbConnection();
        
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);

        // 데이터 전처리 (UID 파싱 및 해시 생성)
        var items = data.Select(kvp => {
            var parts = kvp.Key.Split(':');
            var streamerUid = parts[0];
            var viewerUid = parts.Length > 1 ? parts[1] : "unknown";
            
            return new {
                StreamerUid = streamerUid,
                ViewerUid = viewerUid,
                Hash = Sha256Hasher.ComputeHash(viewerUid),
                Nickname = kvp.Value.Nickname,
                Amount = kvp.Value.Amount
            };
        }).Where(x => x.StreamerUid != "unknown" && x.ViewerUid != "unknown").ToList();

        if (items.Count == 0) return;

        const int chunkSize = 500;
        for (int i = 0; i < items.Count; i += chunkSize)
        {
            var chunk = items.Skip(i).Take(chunkSize).ToList();
            using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                // Step 1: Global Viewers Bulk Upsert
                const string globalSql = @"
                    INSERT INTO core_global_viewers (viewer_uid, viewer_uid_hash, nickname, is_deleted, created_at, updated_at)
                    VALUES (@ViewerUid, @Hash, @Nickname, 0, NOW(), NOW())
                    ON DUPLICATE KEY UPDATE nickname = VALUES(nickname), updated_at = NOW();";
                await connection.ExecuteAsync(globalSql, chunk, transaction);

                // Step 2: Relations Bulk Upsert
                const string relationUpsertSql = @"
                    INSERT INTO core_viewer_relations (streamer_profile_id, global_viewer_id, nickname, is_active, is_deleted, attendance_count, consecutive_attendance_count, first_visit_at, last_chat_at, created_at, updated_at)
                    SELECT s.id, g.id, @Nickname, 1, 0, 0, 0, NOW(), NOW(), NOW(), NOW()
                    FROM core_streamer_profiles s
                    JOIN core_global_viewers g ON g.viewer_uid_hash = @Hash
                    WHERE s.chzzk_uid = @StreamerUid
                    ON DUPLICATE KEY UPDATE nickname = VALUES(nickname), last_chat_at = NOW(), updated_at = NOW();";
                await connection.ExecuteAsync(relationUpsertSql, chunk, transaction);

                // Step 3: Points Bulk Upsert
                const string pointUpsertSql = @"
                    INSERT INTO func_viewer_points (streamer_profile_id, global_viewer_id, points, created_at, updated_at)
                    SELECT s.id, g.id, @Amount, NOW(), NOW()
                    FROM core_streamer_profiles s
                    JOIN core_global_viewers g ON g.viewer_uid_hash = @Hash
                    WHERE s.chzzk_uid = @StreamerUid
                    ON DUPLICATE KEY UPDATE points = points + VALUES(points), updated_at = NOW();";
                await connection.ExecuteAsync(pointUpsertSql, chunk, transaction);

                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "[WriteBack] DB 처리 중 오류 발생 (Rollback): {Msg}", ex.Message);
                throw; // 상위로 던져서 스냅샷이 유지되도록 함
            }
        }
    }
}