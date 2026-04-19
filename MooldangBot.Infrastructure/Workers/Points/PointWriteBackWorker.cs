using System.Text.Json;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Point.Interfaces;
using MooldangBot.Domain.Common.Services; // PulseService, ChaosManager
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Dapper;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Contracts.Chzzk;
using System.Collections.Concurrent;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Point;
using MediatR;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Domain.Common.Security;

namespace MooldangBot.Infrastructure.Workers.Points;

/// <summary>
/// [통합 오버드라이브 워커]: 10K TPS 환경을 위한 포인트 지연 쓰기(Write-Back) 엔진입니다.
/// Redis의 변동분을 벌크로 MariaDB에 반영하며, DB 장애 시 로컬 파일로 덤프하여 무손실을 보장합니다.
/// </summary>
public class PointWriteBackWorker(
    IServiceProvider serviceProvider, 
    PulseService pulse,
    ChaosManager chaosManager,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<PointWriteBackWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(PointWriteBackWorker);
    // [익산 보험] 백업 파일 경로 (Dictionary<string, int> 형태 저장)
    private const string BackupFileName = "data/point_writeback_backup.json"; 
    
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (ex, timeSpan, context) => {
                logger.LogWarning("⚠️ [WriteBack] DB 업데이트 1시적 실패. {TimeSpan}초 후 재시도합니다.", timeSpan.Seconds);
            });

    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [{WorkerName}] 가동 시작 (설정: {Interval}s, 10K TPS 모드)", WorkerName, CurrentSettings.IntervalSeconds);

        // 1. [익산 보험] 기동 시 미처리 파일 복구 시도
        await RestoreBackupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = CurrentSettings;
            if (!settings.IsEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            pulse.ReportPulse(WorkerName);

            // [심연의 시련] 가상 장애 시뮬레이션
            if (chaosManager.IsRedisPanic)
            {
                logger.LogWarning("🌪️ [심연의 시련] Redis 장애 모드 활성화. 동기화를 일시 중단합니다.");
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
                continue;
            }

            await ProcessSyncAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("⏳ [{WorkerName}] 종료 절차 시작 - 잔여 데이터를 DB에 강제 플러시합니다.", WorkerName);
        await ProcessSyncAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessSyncAsync(CancellationToken ct)
    {
        IDictionary<string, PointVariant>? increments = null;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IPointCacheService>();

            // 1. Redis에서 모든 증분 데이터 원자적 추출 (Lua Script) - [포인트 + 닉네임]
            increments = await cache.ExtractAllIncrementalPointsAsync();
            if (increments == null || increments.Count == 0) 
            {
                // [물멍]: 변동분이 없으면 동기화를 건너뛰되, 생존 신고는 남깁니다.
                logger.LogDebug("📡 [WriteBack] 추출된 변동분 없음. 다음 주기를 대기합니다.");
                return;
            }

            logger.LogInformation("🔄 [WriteBack] {Count}건의 포인트 변동분 동기화를 시작합니다.", increments.Count);

            // 2. DB 벌크 업데이트 실행 (Polly 적용)
            await SyncToDatabaseAsync(increments, scope, ct);
            
            logger.LogInformation("✅ [WriteBack] {Count}건의 포인트 및 시청자 정보 동기화 완료.", increments.Count);
        }
        catch (Exception ex)
        {
            // DB 재시도까지 모두 실패했을 경우 (최후의 보루)
            logger.LogCritical(ex, "🚨 [WriteBack] DB 동기화 최종 실패! 데이터를 로컬 파일로 대피시킵니다.");
            
            if (increments != null && increments.Count > 0)
            {
                // [익산 보험] 실패한 데이터 덤프 로직 (DTO 변경에 따른 수정 필요 시 수정)
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
                // [오버드라이브 통합 UPSERT]: 10K TPS를 버티는 3단계 무손실 동기화 엔진
                
                // [Step 0] GlobalViewer 확보 (마스터 정보가 없으면 자동 생성)
                const string globalUpsertSql = @"
                    INSERT INTO core_global_viewers (
                        viewer_uid, viewer_uid_hash, nickname, 
                        is_deleted, created_at, updated_at
                    )
                    VALUES (@ViewerUid, @Hash, @Nickname, 0, NOW(), NOW())
                    AS new_v
                    ON DUPLICATE KEY UPDATE 
                        nickname = new_v.nickname,
                        is_deleted = 0,
                        updated_at = NOW();";

                // [Step 1] ViewerRelation 확보 (관계 및 활동 시간 동기화)
                const string relationUpsertSql = @"
                    INSERT INTO viewer_relations (
                        streamer_profile_id, global_viewer_id, nickname, 
                        is_active, is_deleted, 
                        attendance_count, consecutive_attendance_count,
                        first_visit_at, last_chat_at, 
                        created_at, updated_at
                    )
                    SELECT s.id, g.id, @Nickname, 1, 0, 0, 0, NOW(), NOW(), NOW(), NOW()
                    FROM core_streamer_profiles s
                    JOIN core_global_viewers g ON g.viewer_uid_hash = @Hash
                    WHERE LOWER(s.chzzk_uid) = LOWER(@StreamerUid)
                    AS new_r
                    ON DUPLICATE KEY UPDATE 
                        nickname = new_r.nickname,
                        is_active = 1,
                        is_deleted = 0,
                        last_chat_at = NOW(),
                        updated_at = NOW();";

                // [Step 2] ViewerPoint 정산 (최종 포인트 합산)
                const string pointUpsertSql = @"
                    INSERT INTO viewer_points (
                        streamer_profile_id, global_viewer_id, points, 
                        created_at, updated_at
                    )
                    SELECT s.id, g.id, @Amount, NOW(), NOW()
                    FROM core_streamer_profiles s
                    JOIN core_global_viewers g ON g.viewer_uid_hash = @Hash
                    WHERE LOWER(s.chzzk_uid) = LOWER(@StreamerUid)
                    AS new_p
                    ON DUPLICATE KEY UPDATE 
                        points = points + new_p.points,
                        updated_at = NOW();";

                foreach (var kvp in data)
                {
                    var parts = kvp.Key.Split(':'); // "streamerUid:viewerUid"
                    if (parts.Length < 2) continue;

                    var viewerUid = parts[1];
                    var hash = Sha256Hasher.ComputeHash(viewerUid);

                    var param = new 
                    { 
                        StreamerUid = parts[0], 
                        ViewerUid = viewerUid,
                        Hash = hash,
                        Nickname = kvp.Value.Nickname,
                        Amount = kvp.Value.Amount 
                    };

                    // 1. 마스터 먼저 (Global Identity)
                    await connection.ExecuteAsync(globalUpsertSql, param, transaction);
                    // 2. 관계 정의 (Registration)
                    await connection.ExecuteAsync(relationUpsertSql, param, transaction);
                    // 3. 마지막으로 포인트 (Balance)
                    await connection.ExecuteAsync(pointUpsertSql, param, transaction);
                }

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw; // Polly가 잡아서 재시도하도록 throw
            }
        });
    }

    // ==========================================
    // [익산 보험] (Iksan Insurance) 파일 기반 복구 로직
    // ==========================================
    private async Task CreateBackupAsync(IDictionary<string, PointVariant> failedData)
    {
        try
        {
            var dir = Path.GetDirectoryName(BackupFileName);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // 기존 백업 파일이 있다면 데이터를 병합(Merge)합니다.
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
            logger.LogCritical("💾 [익산 보험] {Count}건의 사용자 포인트 변동분을 안전하게 파일({File})로 저장했습니다.", failedData.Count, BackupFileName);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "🚨 [익산 보험] 파일 덤프마저 실패했습니다! 포인트 유실 위험 발생.");
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
                logger.LogInformation("📦 [익산 보험] 파일에서 {Count}건의 미처리 포인트를 발견했습니다. DB 복구를 시도합니다.", backupData.Count);
                
                using var scope = serviceProvider.CreateScope();
                await SyncToDatabaseAsync(backupData, scope, ct);
                
                logger.LogInformation("✅ [익산 보험] 파일 백업 데이터 DB 복구 완료.");
            }
            // 복구 성공 시 파일 삭제
            File.Delete(BackupFileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [익산 보험] 백업 파일 복구 중 오류 발생. 다음 주기에 다시 시도하거나 수동 조치가 필요합니다.");
        }
    }
}