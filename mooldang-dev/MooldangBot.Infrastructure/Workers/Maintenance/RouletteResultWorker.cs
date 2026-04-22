using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Modules.Roulette.Features.Queries.ProcessTimeout;
using MediatR;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

/// <summary>
/// [오시리스의 파수꾼]: 룰렛 결과 전송 스케줄을 감시하고 자동 전송을 수행하는 백그라운드 서비스입니다.
/// (Phase 3): 순환 참조 방지를 위해 Infrastructure 계층으로 안착되었습니다.
/// </summary>
public class RouletteResultWorker(IServiceProvider serviceProvider,
    
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<RouletteResultWorker> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(RouletteResultWorker))
{
    // [지휘관 지침]: 룰렛 결과 감시 주기는 기본 10초로 설정합니다.
    protected override int DefaultIntervalSeconds => 10;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // [순수 수직 분할]: 타임아웃 처리를 독립 모듈에 요청
        await mediator.Send(new ProcessTimeoutSpinsCommand(), ct);
    }
}
