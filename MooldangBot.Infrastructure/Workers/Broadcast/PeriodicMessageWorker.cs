using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Application.Features.Broadcast;

namespace MooldangBot.Infrastructure.Workers.Broadcast;

/// <summary>
/// [정기 메시지 방아쇠]: 설정된 주기마다 스트리머별 정기 메시지 송출 명령을 방송 모듈로 하달합니다.
/// </summary>
public class PeriodicMessageWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<PeriodicMessageWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(PeriodicMessageWorker);

    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [PeriodicMessageWorker] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

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
                
                // 방송 모듈로 송출 명령 하달
                await mediator.Send(new SendPeriodicMessagesCommand(), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [PeriodicMessageWorker] 실행 중 오류 발생");
            }

            // [설정]: 정기 메시지 체크 주기 반영 (기본 60초)
            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
        }
    }
}
