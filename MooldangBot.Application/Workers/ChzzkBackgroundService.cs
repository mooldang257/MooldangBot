using Microsoft.Extensions.Hosting;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Workers;

public class ChzzkBackgroundService : BackgroundService
{
    private readonly ILogger<ChzzkBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IChzzkApiClient _chzzkApi;

    public ChzzkBackgroundService(ILogger<ChzzkBackgroundService> logger, IServiceProvider serviceProvider, IChzzkApiClient chzzkApi)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _chzzkApi = chzzkApi;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [치지직 백그라운드 서비스] 가동 중...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();

                var profiles = await db.StreamerProfiles.Where(p => p.IsBotEnabled).ToListAsync(stoppingToken);

                foreach (var profile in profiles)
                {
                    // 봇 채널 관전 및 연결 로직...
                    // (ChzzkChannelWorker 팩토리를 통해 처리)
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [치지직 백그라운드 서비스] 실행 중 오류 발생");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
