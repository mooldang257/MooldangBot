using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;

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
            int nextDelayMs = 2000; // 활성 상태 기본 2초

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var rouletteService = scope.ServiceProvider.GetRequiredService<IRouletteService>();

                // [v1.9.9.2] 스마트 폴링: 미완료된 세션이 있는지 가볍게 확인
                var hasPending = await db.RouletteSpins.AnyAsync(s => !s.IsCompleted, stoppingToken);

                if (hasPending)
                {
                    // 활성 상태: 2초 단위 정밀 감시
                    var overdueSpins = await db.RouletteSpins
                        .Where(s => !s.IsCompleted && s.ScheduledTime <= DateTime.Now)
                        .OrderBy(s => s.ScheduledTime)
                        .Take(10)
                        .ToListAsync(stoppingToken);

                    foreach (var spin in overdueSpins)
                    {
                        await rouletteService.CompleteRouletteAsync(spin.Id);
                    }
                }
                else
                {
                    // 휴면 상태: 30초 단위 저부하 감시 (사용자 요청)
                    nextDelayMs = 30000;
                }
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
