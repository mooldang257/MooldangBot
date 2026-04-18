using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Infrastructure.Workers;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

/// <summary>
/// [v13.1] 1개월이 경과한 스테이징 노래 데이터를 자동으로 정리하는 백그라운드 서비스입니다.
/// </summary>
public class StagingCleanupWorker(
    IServiceProvider serviceProvider, 
    IOptionsMonitor<WorkerSettings> optionsMonitor, // [수정] Named Options 패턴 적용
    ILogger<StagingCleanupWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(StagingCleanupWorker);

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [StagingCleanupWorker] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = CurrentSettings;
            if (!settings.IsEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                logger.LogDebug("[SongBook] 스테이징 데이터 정리 작업을 시작합니다.");
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                // 기준 시간: 현재로부터 30일 이전
                var cutoffDate = DateTime.UtcNow.AddDays(-30);

                // [v13.1] EF Core 7+ ExecuteDeleteAsync를 활용한 고성능 벌크 삭제
                int deletedCount = await dbContext.MasterSongStagings
                    .Where(s => s.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deletedCount > 0)
                {
                    logger.LogInformation("[SongBook] 만료된 스테이징 데이터 {Count}건 삭제 완료. (기준: {Cutoff})", deletedCount, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SongBook] 스테이징 데이터 정리 중 예외가 발생했습니다.");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
        }
    }
}
