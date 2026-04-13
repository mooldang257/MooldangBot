using Microsoft.Extensions.Hosting;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Workers;

public class RouletteLogCleanupService : BackgroundService
{
    private readonly ILogger<RouletteLogCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RouletteLogCleanupService(ILogger<RouletteLogCleanupService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                var thresholdDate = KstClock.Now.AddDays(-7);
                var oldLogs = await EntityFrameworkQueryableExtensions.ExecuteDeleteAsync(
                    db.RouletteLogs.Where(l => l.CreatedAt < thresholdDate), 
                    stoppingToken);

                if (oldLogs > 0)
                {
                    _logger.LogInformation($"🧹 [룰렛 로그 정리] {oldLogs}개의 오래된 로그를 삭제했습니다.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [룰렛 로그 정리] 실행 중 오류 발생");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
