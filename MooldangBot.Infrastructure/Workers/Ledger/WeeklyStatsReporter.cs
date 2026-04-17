using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.Ledger.Features.Commands;

namespace MooldangBot.Infrastructure.Workers.Ledger;

/// <summary>
/// [주간 결산 방아쇠]: 매주 월요일 오전 9시에 통계 리포트 생성 명령을 결산 모듈로 하달합니다.
/// </summary>
public class WeeklyStatsReporter(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<WeeklyStatsReporter> logger) : BackgroundService
{
    private const string WorkerName = nameof(WeeklyStatsReporter);
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [WeeklyStatsReporter] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

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
                var now = KstClock.Now;
                
                // 매주 월요일 오전 9시 체성분 분석 (설정된 주기에 따라 체크)
                if (now.Value.DayOfWeek == DayOfWeek.Monday && now.Value.Hour == 9)
                {
                    using var scope = scopeFactory.CreateScope();
                    var pulse = scope.ServiceProvider.GetRequiredService<PulseService>();
                    var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

                    pulse.ReportPulse(WorkerName);
                    logger.LogInformation("🔔 [주간 결산] 월요일 아침이 되었습니다. 리포트 생성을 하달합니다.");

                    // 결산 모듈로 리포트 생성 명령 하달
                    await mediator.Send(new GenerateWeeklyStatsReportCommand(), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [WeeklyStatsReporter] 실행 중 오류 발생");
            }

            // 체크 주기는 설정을 따르거나 고정값(30분) 사용
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
