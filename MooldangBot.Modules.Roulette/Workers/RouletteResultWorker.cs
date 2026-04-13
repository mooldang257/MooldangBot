using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Roulette.Features.Queries.ProcessTimeout;
using MediatR;

namespace MooldangBot.Modules.Roulette.Workers;

/// <summary>
/// [오시리스의 파수꾼]: 룰렛 결과 전송 스케줄(영속화)을 감시하고, 
/// 오버레이가 응답하지 않거나 장기 세션인 경우 결과를 자동 전송하는 백그라운드 서비스입니다.
/// </summary>
public class RouletteResultWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RouletteResultWorker> _logger;

    public RouletteResultWorker(IServiceScopeFactory scopeFactory, ILogger<RouletteResultWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [RouletteResultWorker] 지능형 광대역 파수꾼이 가동되었습니다.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // 📡 [순수 수직 분할]: 타임아웃 처리를 독립 모듈에 요청합니다.
                await mediator.Send(new ProcessTimeoutSpinsCommand(), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RouletteResultWorker] 감시 중 오류 발생");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
