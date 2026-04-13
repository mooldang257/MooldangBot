using Microsoft.Extensions.Hosting;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [RETIRED]: 이 서비스는 Phase 2 'Egyptian Bridge' 아키텍처 도입으로 은퇴하였습니다.
/// 모든 기능은 ChzzkEventRabbitMqConsumer와 ChzzkEventProcessingWorker로 이관되었습니다.
/// </summary>
public sealed class ChatEventConsumerService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
