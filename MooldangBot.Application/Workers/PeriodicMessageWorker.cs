using Microsoft.Extensions.Hosting;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Workers;

public class PeriodicMessageWorker : BackgroundService
{
    private readonly ILogger<PeriodicMessageWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOverlayNotificationService _overlayService;

    public PeriodicMessageWorker(ILogger<PeriodicMessageWorker> logger, IServiceProvider serviceProvider, IOverlayNotificationService overlayService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _overlayService = overlayService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [주기적 메시지 워커] 가동 중...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
                var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();

                var profiles = await db.StreamerProfiles.Where(p => p.IsBotEnabled).ToListAsync(stoppingToken);

                foreach (var profile in profiles)
                {
                    // 주기적 메시지 로직...
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [주기적 메시지 워커] 실행 중 오류 발생");
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
