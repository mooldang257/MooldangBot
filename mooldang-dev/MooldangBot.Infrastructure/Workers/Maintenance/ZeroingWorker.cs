using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Application.Features.Core;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

/// <summary>
/// [영점 조절 방아쇠]: 전역 접속자 카운트 및 함대 동기화 명령을 핵심 모듈로 하달합니다.
/// </summary>
public class ZeroingWorker(IServiceProvider serviceProvider,
    
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<ZeroingWorker> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(ZeroingWorker))
{
    // [지휘관 지침]: 영점 조절 주기는 기본 6시간(21,600초)으로 설정합니다.
    protected override int DefaultIntervalSeconds => 21600;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        using var Scope = scopeFactory.CreateScope();
        var Pulse = Scope.ServiceProvider.GetRequiredService<PulseService>();
        var Mediator = Scope.ServiceProvider.GetRequiredService<ISender>();
 
        _logger.LogInformation("🛡️ [영점 조절] 함대 동기화 명령을 하달합니다.");
        await Mediator.Send(new SyncFleetConnectionsCommand(), ct);
    }
}
