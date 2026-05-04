using MooldangBot.Foundation.Services;
using MooldangBot.Foundation.Workers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Application.Features.Broadcast;

namespace MooldangBot.Infrastructure.Workers.Broadcast;

/// <summary>
/// [정기 메시지 방아쇠]: 설정된 주기마다 스트리머별 정기 메시지 송출 명령을 방송 모듈로 하달합니다.
/// </summary>
public class PeriodicMessageWorker(IServiceProvider serviceProvider,
    
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<PeriodicMessageWorker> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(PeriodicMessageWorker))
{
    // [지휘관 지침]: 정기 메시지 체크 주기는 기본 60초로 설정합니다.

    protected override bool RequiresDistributedLock => true;

    protected override int DefaultIntervalSeconds => 60;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var pulse = scope.ServiceProvider.GetRequiredService<PulseService>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        _logger.LogInformation("📢 [정기 메시지] 방송 모듈로 송출 명령을 하달합니다.");
        await mediator.Send(new SendPeriodicMessagesCommand(), ct);
    }
}
