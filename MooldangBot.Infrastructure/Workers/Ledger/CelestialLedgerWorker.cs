using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.Point.Features.Commands;
using MooldangBot.Modules.Roulette.Features.Commands;

namespace MooldangBot.Infrastructure.Workers.Ledger;

/// <summary>
/// [천상의 장부 방아쇠]: 6시간마다 통계 집계 명령을 각 모듈로 하달하는 스케줄러입니다.
/// </summary>
public class CelestialLedgerWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<CelestialLedgerWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(CelestialLedgerWorker);

    // [수정] Named Options Get(WorkerName)으로 설정 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [CelestialLedgerWorker] 가동 준비 완료 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

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
                // 1. 설정된 주기만큼 대기
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);

                // 2. 시간 도래 시 스코프 생성 및 MediatR 호출
                using var scope = scopeFactory.CreateScope();
                var pulse = scope.ServiceProvider.GetRequiredService<PulseService>();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

                pulse.ReportPulse(WorkerName);
                logger.LogInformation("🔔 [천상의 장부] 정기 결산 시간이 도래했습니다. 각 모듈에 명령을 하달합니다.");

                // 각 모듈의 Handler로 명령 하달 (결합도 최소화)
                await mediator.Send(new AggregatePointStatsCommand(), stoppingToken);
                await mediator.Send(new AggregateRouletteStatsCommand(), stoppingToken);
                await mediator.Send(new CleanupExpiredLogsCommand(), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [CelestialLedgerWorker] 스케줄링 루프 중 오류 발생");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
