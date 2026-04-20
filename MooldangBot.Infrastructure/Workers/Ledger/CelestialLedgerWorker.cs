using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Modules.Point.Features.Commands;
using MooldangBot.Modules.Roulette.Features.Commands;

namespace MooldangBot.Infrastructure.Workers.Ledger;

/// <summary>
/// [천상의 장부 방아쇠]: 정기 결산 및 통계 집계 명령을 각 모듈로 하달하는 지휘관 워커입니다.
/// </summary>
public class CelestialLedgerWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<CelestialLedgerWorker> logger) : BaseHybridWorker(logger, optionsMonitor, nameof(CelestialLedgerWorker))
{
    // [지휘관 지침]: 기본 결산 주기는 6시간(21,600초)으로 설정합니다.
    protected override int DefaultIntervalSeconds => 21600;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var pulse = scope.ServiceProvider.GetRequiredService<PulseService>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        pulse.ReportPulse(_workerName);
        _logger.LogInformation("🔔 [천상의 장부] 정기 결산 및 데이터 무결성 검사 명령을 하달합니다.");

        // 각 모듈의 Handler로 명령 하달 (모듈 간 직접 참조 없이 MediatR로 통신)
        await mediator.Send(new AggregatePointStatsCommand(), ct);
        await mediator.Send(new AggregateRouletteStatsCommand(), ct);
        await mediator.Send(new CleanupExpiredLogsCommand(), ct);
    }
}
