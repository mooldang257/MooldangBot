using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Features.Ledger;

namespace MooldangBot.Infrastructure.Workers.Ledger;

/// <summary>
/// [주간 결산 방아쇠]: 매주 월요일 오전 9시에 통계 리포트 생성 명령을 결산 모듈로 하달합니다.
/// </summary>
public class WeeklyStatsReporter(IServiceProvider serviceProvider,
    
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<WeeklyStatsReporter> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(WeeklyStatsReporter))
{
    // [지휘관 지침]: 주간 리포트 체크는 30분(1,800초) 단위로 수행합니다.
    protected override int DefaultIntervalSeconds => 1800;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        var now = KstClock.Now;
        
        // 매주 월요일 오전 9시 체성분 분석
        if (now.Value.DayOfWeek == DayOfWeek.Monday && now.Value.Hour == 9)
        {
            using var scope = scopeFactory.CreateScope();
            var pulse = scope.ServiceProvider.GetRequiredService<PulseService>();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            _logger.LogInformation("🔔 [주간 결산] 월요일 아침이 되었습니다. 리포트 생성을 하달합니다.");

            await mediator.Send(new GenerateWeeklyStatsReportCommand(), ct);
        }
    }
}
