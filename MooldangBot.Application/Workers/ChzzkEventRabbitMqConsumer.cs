using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Events;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Events;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Models.Chat;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [v3.7 오시리스의 전령]: RabbitMQ로부터 치지직 이벤트를 수신하여 메인 앱의 MediatR 파이프라인으로 전달합니다.
/// </summary>
public sealed class ChzzkEventRabbitMqConsumer : BackgroundService
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly IChatEventChannel _bridge;
    private readonly ILogger<ChzzkEventRabbitMqConsumer> _logger;

    private const string QueueName = "mooldang.app.chzzk.events";

    public ChzzkEventRabbitMqConsumer(
        IRabbitMqService rabbitMq,
        IChatEventChannel bridge,
        ILogger<ChzzkEventRabbitMqConsumer> logger)
    {
        _rabbitMq = rabbitMq;
        _bridge = bridge;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [이벤트 수집기] RabbitMQ 구독 개시 (Bridge 채널 연결 완료): {QueueName}", QueueName);

        try
        {
            await _rabbitMq.SubscribeAsync(
                RabbitMqExchanges.ChatEvents,
                QueueName,
                "streamer.#", 
                OnMessageReceivedAsync,
                stoppingToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "💀 [이벤트 수집기] 구독 중 치명적 오류 발생");
        }
    }

    private async Task OnMessageReceivedAsync(string message, string routingKey)
    {
        // [오시리스의 Bridge]: Singleton 환경에서 최소한의 파싱만 수행하여 즉시 Channel로 넘깁니다.
        try
        {
            // 1. [고속 메타데이터 추출]: 전체 역직렬화 대신 필요한 필드만 신속 탐색
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            // [오시리스의 눈]: 발행자(ChzzkAPI)의 PascalCase 규격에 맞춰 추출 (CamelCase 대비용 Fallback 포함)
            if (!root.TryGetProperty("MessageId", out var idProp) && !root.TryGetProperty("messageId", out idProp))
            {
                _logger.LogWarning("⚠️ [이벤트 수집기] MessageId 프로퍼티를 찾을 수 없습니다. (RoutingKey: {RoutingKey})", routingKey);
                return;
            }

            if (!root.TryGetProperty("ChzzkUid", out var uidProp) && !root.TryGetProperty("chzzkUid", out uidProp))
            {
                _logger.LogWarning("⚠️ [이벤트 수집기] ChzzkUid 프로퍼티를 찾을 수 없습니다. (RoutingKey: {RoutingKey})", routingKey);
                return;
            }

            if (!root.TryGetProperty("Payload", out var payloadProp) && !root.TryGetProperty("payload", out payloadProp))
            {
                _logger.LogWarning("⚠️ [이벤트 수집기] Payload 프로퍼티를 찾을 수 없습니다. (RoutingKey: {RoutingKey})", routingKey);
                return;
            }

            // 2. [오시리스의 패킷]: record struct로 변환하여 힙 할당 없이 복사
            var packet = new ChatEventPacket(
                idProp.GetGuid(),
                uidProp.GetString() ?? "",
                "UNKNOWN", // 처리 레이어에서 구체화 가능
                payloadProp.Clone(), // JsonDocument가 Dispose되므로 Clone 필요 (할당은 발생하지만 JsonDocument 전체보다는 작음)
                DateTime.UtcNow
            );

            if (!_bridge.TryWrite(packet))
            {
                _logger.LogWarning("⚠️ [Bridge 포화] 메시지 {MessageId}를 드랍합니다.", packet.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "❌ [이벤트 수집기] 메시지 파싱 실패 (RoutingKey: {RoutingKey})", routingKey);
        }
    }
}
