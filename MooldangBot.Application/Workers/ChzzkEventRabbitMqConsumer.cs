using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Events;
using MooldangBot.Application.Interfaces;
using MooldangBot.ChzzkAPI.Contracts;
using MooldangBot.ChzzkAPI.Contracts.Models.Events;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [v3.7 오시리스의 전령]: RabbitMQ로부터 치지직 이벤트를 수신하여 메인 앱의 MediatR 파이프라인으로 전달합니다.
/// </summary>
public sealed class ChzzkEventRabbitMqConsumer : BackgroundService
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChzzkEventRabbitMqConsumer> _logger;

    private const string QueueName = "mooldang.app.chzzk.events";

    public ChzzkEventRabbitMqConsumer(
        IRabbitMqService rabbitMq,
        IServiceScopeFactory scopeFactory,
        ILogger<ChzzkEventRabbitMqConsumer> logger)
    {
        _rabbitMq = rabbitMq;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [이벤트 소비자] RabbitMQ 구독 개시: {QueueName}", QueueName);

        try
        {
            await _rabbitMq.SubscribeAsync(
                RabbitMqExchanges.ChatEvents,
                QueueName,
                "streamer.#", // 모든 스트리머의 채팅/후원 이벤트 수신
                OnMessageReceivedAsync,
                stoppingToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "💀 [이벤트 소비자] 구독 중 치명적 오류 발생");
        }
    }

    private async Task OnMessageReceivedAsync(string message, string routingKey)
    {
        try
        {
            // [오시리스의 직렬화]: 소스 생성기 기반의 고속 역직렬화 수행
            var envelope = JsonSerializer.Deserialize<ChzzkEventEnvelope>(message, ChzzkJsonContext.Default.ChzzkEventEnvelope);
            
            if (envelope == null)
            {
                _logger.LogWarning("⚠️ [이벤트 소비자] 빈 봉투가 수신되었습니다. (RoutingKey: {RoutingKey})", routingKey);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var mediatr = scope.ServiceProvider.GetRequiredService<IMediator>();
            var identityCache = scope.ServiceProvider.GetRequiredService<IIdentityCacheService>();

            // 1. [파로스의 자각]: 캐시에서 스트리머 프로필 확인
            var profile = await identityCache.GetStreamerProfileAsync(envelope.ChzzkUid);
            if (profile == null)
            {
                _logger.LogWarning("⚠️ [이벤트 소비자] 알 수 없는 스트리머의 이벤트입니다: {ChzzkUid}", envelope.ChzzkUid);
                return;
            }

            // 2. [오시리스의 중재]: MediatR을 통해 도메인 이벤트로 전파
            _logger.LogInformation("📨 [이벤트 소비자] {Type} 수신: {ChzzkUid} (MessageId: {MessageId})", 
                envelope.Payload.GetType().Name, envelope.ChzzkUid, envelope.MessageId);

            await mediatr.Publish(new ChzzkEventReceived(
                envelope.MessageId,
                profile,
                envelope.Payload,
                envelope.ReceivedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [이벤트 소비자] 메시지 처리 실패: {RoutingKey}", routingKey);
        }
    }
}
