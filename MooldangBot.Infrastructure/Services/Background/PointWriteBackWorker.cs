using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Dapper;

namespace MooldangBot.Infrastructure.Services.Background;

/// <summary>
/// [v7.0] 포인트 지연 쓰기 워커: Redis에 쌓인 비유료 포인트 변동분을 
/// 1분 주기로 MariaDB에 벌크 업서트(Bulk Upsert)하여 부하를 최소화합니다.
/// </summary>
public class PointWriteBackWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PointWriteBackWorker> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public PointWriteBackWorker(IServiceProvider serviceProvider, ILogger<PointWriteBackWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // [오시리스의 인내]: DB 일시 장애 대비 지수 백오프 재시도 정책
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, timeSpan, context) => {
                    _logger.LogWarning(ex, "⚠️ [WriteBack] DB 업데이트 실패. {TimeSpan}초 후 재시도합니다.", timeSpan.Seconds);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [PointWriteBackWorker] 가동 시작 (Cycle: 1min / Pattern: Write-Back)");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessSyncAsync(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏳ [PointWriteBackWorker] 종료 절차 시작 - 잔여 데이터 강제 플러시(Flush)를 시도합니다.");
        await ProcessSyncAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessSyncAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IPointCacheService>();
            var db = scope.ServiceProvider.GetRequiredService<IPointDbContext>();

            // 1. Redis에서 모든 증분 데이터 원자적 추출 (Lua Script 기반)
            var increments = await cache.ExtractAllIncrementalPointsAsync();
            if (increments.Count == 0) return;

            var notification = scope.ServiceProvider.GetService<INotificationService>();
            
            _logger.LogInformation("📊 [WriteBack] {Count}건의 포인트 변동분을 DB에 병합합니다.", increments.Count);

            // 2. Polly 재시도 정책 하에 DB 벌크 업데이트 실행
            await _retryPolicy.ExecuteAsync(async () => 
            {
                var connection = db.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct);

                using var transaction = await connection.BeginTransactionAsync(ct);
                try
                {
                    // [물멍의 일격]: Bulk Upsert (MariaDB 전용 문법)
                    const string upsertSql = @"
                        INSERT INTO viewer_points (streamer_profile_id, global_viewer_id, points, created_at, updated_at)
                        SELECT s.id, g.id, @Amount, NOW(), NOW()
                        FROM core_streamer_profiles s
                        JOIN global_viewers g ON g.id = (SELECT id FROM global_viewers WHERE viewer_uid = @ViewerUid LIMIT 1)
                        WHERE s.chzzk_uid = @StreamerUid
                        ON DUPLICATE KEY UPDATE 
                            points = points + VALUES(points),
                            updated_at = NOW();";

                    foreach (var kvp in increments)
                    {
                        var parts = kvp.Key.Split(':'); // "streamerUid:viewerUid"
                        if (parts.Length < 2) continue;

                        await connection.ExecuteAsync(upsertSql, new 
                        { 
                            StreamerUid = parts[0], 
                            ViewerUid = parts[1], 
                            Amount = kvp.Value 
                        }, transaction);
                    }

                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });

            _logger.LogInformation("✅ [WriteBack] {Count}건의 포인트 동기화가 성공적으로 완료되었습니다.", increments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "🚨 [WriteBack] 치명적 오류 발생! 처리되지 못한 데이터는 DLQ(failed_points_queue) 검토가 필요합니다.");
            
            using var scope = _serviceProvider.CreateScope();
            var notification = scope.ServiceProvider.GetService<INotificationService>();
            if (notification != null)
            {
                await notification.SendAlertAsync(
                    $"🚨 [PointWriteBack] DB 동기화 실패 및 데이터 유실 위험!\n{ex.Message}", 
                    isCritical: true, 
                    alertKey: "point-writeback-fail");
            }
        }
    }
}
