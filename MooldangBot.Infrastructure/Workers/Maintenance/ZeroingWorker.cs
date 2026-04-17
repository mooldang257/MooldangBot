using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.Core.Features.Commands;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

/// <summary>
/// [영점 조절 방아쇠]: 전역 접속자 카운트 등의 동기화 명령을 핵심 모듈로 하달합니다.
/// </summary>
public class ZeroingWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<ZeroingWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(ZeroingWorker);

    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🛡️ [ZeroingWorker] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

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
                using var scope = scopeFactory.CreateScope();
                var pulse = scope.ServiceProvider.GetRequiredService<PulseService>();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

                pulse.ReportPulse(WorkerName);
                
                // 핵심 모듈로 영점 조절 명령 하달
                await mediator.Send(new SyncFleetConnectionsCommand(), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [ZeroingWorker] 실행 중 오류 발생");
            }

            // [설정]: 영점 조절 주기 반영 (기본 6시간)
            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
        }
    }
}
