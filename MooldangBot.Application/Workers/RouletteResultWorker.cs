using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;

using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [오시리스의 파수꾼]: 룰렛 결과 전송 스케줄(영속화)을 감시하고, 
/// 오버레이가 응답하지 않거나 장기 세션인 경우 결과를 자동 전송하는 백그라운드 서비스입니다. (v1.9.9)
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
        _logger.LogInformation("🚀 [RouletteResultWorker] 스마트 파수꾼이 가동되었습니다. (v1.9.9.2)");

        while (!stoppingToken.IsCancellationRequested)
        {
            int nextDelayMs = 10000; // [오시리스의 인내]: 부하 및 로그 소음 감소를 위해 10초로 상향 (v2.2.1)

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var rouletteService = scope.ServiceProvider.GetRequiredService<IRouletteService>();

                // [v2.2.0] 지능형 타임아웃 처리 위임
                await rouletteService.ProcessTimeoutSpinsAsync(stoppingToken);
                
                // 유휴 상태 확인은 서비스 내에서 쿼리 최적화가 되어 있으므로 단순 10초 주기로 유지
                nextDelayMs = 10000;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RouletteResultWorker] 감시 중 오류 발생");
                nextDelayMs = 10000;
            }

            await Task.Delay(nextDelayMs, stoppingToken);
        }
    }
}
