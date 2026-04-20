using System.Text.Json;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Point.Interfaces;
using MooldangBot.Domain.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Dapper;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Contracts.Chzzk;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Contracts.Point;
using MooldangBot.Domain.Common.Security;

namespace MooldangBot.Infrastructure.Workers.Points;

/// <summary>
/// [통합 오버드라이브 워커]: 10K TPS 환경을 위한 포인트 지연 쓰기(Write-Back) 엔진입니다.
/// </summary>
public class PointWriteBackWorker(
    IServiceProvider serviceProvider, 
    PulseService pulse,
    ChaosManager chaosManager,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<PointWriteBackWorker> logger) : BaseHybridWorker(logger, optionsMonitor, nameof(PointWriteBackWorker))
{
    private const string BackupFileName = "data/point_writeback_backup.json"; 
    
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (ex, timeSpan, context) => {
                logger.LogWarning("⚠️ [WriteBack] DB 업데이트 1시적 실패. {TimeSpan}초 후 재시도합니다.", timeSpan.Seconds);
            });

    protected override int DefaultIntervalSeconds => 10;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await RestoreBackupAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        pulse.ReportPulse(_workerName);

        if (chaosManager.IsRedisPanic)
        {
            _logger.LogWarning("🌪️ [심연의 시련] Redis 장애 모드 활성화. 동기화를 일시 중단합니다.");
            return;
        }

        await FlushSyncAsync(ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("⏳ [{WorkerName}] 종료 절차 시작 - 잔여 데이터를 DB에 강제 플러시합니다.", _workerName);
        await FlushSyncAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private async Task FlushSyncAsync(CancellationToken ct)
    {
        IDictionary<string, PointVariant>? increments = null;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IPointCacheService>();

            increments = await cache.ExtractAllIncrementalPointsAsync();
            if (increments == null || increments.Count == 0) return;

            _logger.LogInformation("🔄 [WriteBack] {Count}건의 포인트 변동분 동기화를 시작합니다.", increments.Count);
            await SyncToDatabaseAsync(increments, scope, ct);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "🚨 [WriteBack] DB 동기화 최종 실패! 대피 시작.");
            if (increments != null && increments.Count > 0)
            {
                await CreateBackupAsync(increments);
            }
        }
    }

    private async Task SyncToDatabaseAsync(IDictionary<string, PointVariant> data, IServiceScope scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<IPointDbContext>();

        await _retryPolicy.ExecuteAsync(async () => 
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct);

            using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                const string globalUpsertSql = @"
                    INSERT INTO core_global_viewers (viewer_uid, viewer_uid_hash, nickname, is_deleted, created_at, updated_at)
                    VALUES (@ViewerUid, @Hash, @Nickname, 0, NOW(), NOW())
                    ON DUPLICATE KEY UPDATE nickname = VALUES(nickname), updated_at = NOW();";

                const string relationUpsertSql = @"
                    INSERT INTO viewer_relations (streamer_profile_id, global_viewer_id, nickname, is_active, is_deleted, attendance_count, consecutive_attendance_count, first_visit_at, last_chat_at, created_at, updated_at)
                    SELECT s.id, g.id, @Nickname, 1, 0, 0, 0, NOW(), NOW(), NOW(), NOW()
                    FROM core_streamer_profiles s
                    JOIN core_global_viewers g ON g.viewer_uid_hash = @Hash
                    WHERE LOWER(s.chzzk_uid) = LOWER(@StreamerUid)
                    ON DUPLICATE KEY UPDATE nickname = VALUES(nickname), last_chat_at = NOW(), updated_at = NOW();";

                const string pointUpsertSql = @"
                    INSERT INTO viewer_points (streamer_profile_id, global_viewer_id, points, created_at, updated_at)
                    SELECT s.id, g.id, @Amount, NOW(), NOW()
                    FROM core_streamer_profiles s
                    JOIN core_global_viewers g ON g.viewer_uid_hash = @Hash
                    WHERE LOWER(s.chzzk_uid) = LOWER(@StreamerUid)
                    ON DUPLICATE KEY UPDATE points = points + VALUES(points), updated_at = NOW();";

                foreach (var kvp in data)
                {
                    var parts = kvp.Key.Split(':');
                    if (parts.Length < 2) continue;

                    var viewerUid = parts[1];
                    var hash = Sha256Hasher.ComputeHash(viewerUid);
                    var param = new { StreamerUid = parts[0], ViewerUid = viewerUid, Hash = hash, Nickname = kvp.Value.Nickname, Amount = kvp.Value.Amount };

                    await connection.ExecuteAsync(globalUpsertSql, param, transaction);
                    await connection.ExecuteAsync(relationUpsertSql, param, transaction);
                    await connection.ExecuteAsync(pointUpsertSql, param, transaction);
                }
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task CreateBackupAsync(IDictionary<string, PointVariant> failedData)
    {
        try
        {
            var dir = Path.GetDirectoryName(BackupFileName);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(BackupFileName))
            {
                var existingJson = await File.ReadAllTextAsync(BackupFileName);
                var existingData = JsonSerializer.Deserialize<Dictionary<string, PointVariant>>(existingJson) ?? new();
                foreach (var kvp in failedData)
                {
                    if (existingData.ContainsKey(kvp.Key)) 
                    {
                        var existing = existingData[kvp.Key];
                        existingData[kvp.Key] = existing with { Amount = existing.Amount + kvp.Value.Amount, Nickname = kvp.Value.Nickname };
                    }
                    else existingData[kvp.Key] = kvp.Value;
                }
                failedData = existingData;
            }

            var json = JsonSerializer.Serialize(failedData);
            await File.WriteAllTextAsync(BackupFileName, json);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "🚨 [{WorkerName}] 백업 파일 덤프 실패!", _workerName);
        }
    }

    private async Task RestoreBackupAsync(CancellationToken ct)
    {
        if (!File.Exists(BackupFileName)) return;
        try
        {
            var json = await File.ReadAllTextAsync(BackupFileName, ct);
            var backupData = JsonSerializer.Deserialize<Dictionary<string, PointVariant>>(json);
            if (backupData != null && backupData.Count > 0)
            {
                using var scope = serviceProvider.CreateScope();
                await SyncToDatabaseAsync(backupData, scope, ct);
            }
            File.Delete(BackupFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{WorkerName}] 복구 중 오류 발생", _workerName);
        }
    }
}