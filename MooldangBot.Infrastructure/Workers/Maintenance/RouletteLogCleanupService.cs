using Microsoft.Extensions.Hosting;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

public class RouletteLogCleanupService(
    ILogger<RouletteLogCleanupService> logger, 
    IServiceProvider serviceProvider,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BackgroundService
{
    private const string WorkerName = nameof(RouletteLogCleanupService);

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [RouletteLogCleanupService] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

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
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                var thresholdDate = KstClock.Now.AddDays(-7);
                var oldLogs = await db.RouletteLogs
                    .Where(l => l.CreatedAt < thresholdDate)
                    .ExecuteDeleteAsync(stoppingToken);

                if (oldLogs > 0)
                {
                    logger.LogInformation("🧹 [룰렛 로그 정리] {Count}개의 오래된 로그를 삭제했습니다.", oldLogs);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [룰렛 로그 정리] 실행 중 오류 발생");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
        }
    }
}
