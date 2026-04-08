using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using MooldangBot.Domain.Common;
using MooldangBot.Infrastructure.Messaging;
using Serilog.Context;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 청취자]: RabbitMQ의 Topic 익스체인지로부터 치지직 이벤트를 수신하여 리액터(MediatR 등)에 중계하는 서비스입니다.
/// MooldangBot.Api 프로젝트에서 상주하며 봇 엔진으로부터 메시지를 받아 비즈니스 로직을 수행합니다.
/// </summary>
public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQPersistentConnection _connection;
    private readonly string _chatExchange = RabbitMqExchanges.ChatEvents;
    private readonly string _logExchange = RabbitMqExchanges.SystemLogs;
    private IChannel? _channel;

    public RabbitMqConsumerService(
        ILogger<RabbitMqConsumerService> logger, 
        IServiceScopeFactory scopeFactory,
        RabbitMQPersistentConnection connection)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _connection = connection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📡 [청취자의 대기] RabbitMQ Topic(v2.0) 이벤트 구독을 시작합니다.");

        if (!_connection.IsConnected) await _connection.TryConnectAsync();

        _channel = await _connection.CreateModelAsync();

        // 1. [익스체인지 선언] (이미 발행측에서 선언되었으나 안전을 위해 재선언)
        await _channel.ExchangeDeclareAsync(_chatExchange, ExchangeType.Topic, true);
        await _channel.ExchangeDeclareAsync(_logExchange, ExchangeType.Topic, true);

        // 2. [채팅 처리용 큐 설정]
        var queueName = (await _channel.QueueDeclareAsync(queue: "", exclusive: true, autoDelete: true)).QueueName;
        await _channel.QueueBindAsync(queueName, _chatExchange, "streamer.#"); // 모든 스트리머 채팅 구독
        await _channel.QueueBindAsync(queueName, _logExchange, "command.log"); // 명령어 집계 로그 구독

        // 3. [비동기 소비자 구성]
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using var scope = _scopeFactory.CreateScope();
                var mediatr = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

                // [v2.0] 라우팅 키에 따른 지능형 분기
                if (routingKey.StartsWith("streamer."))
                {
                    var eventItem = JsonSerializer.Deserialize<ChatEventItem>(message, jsonOptions);
                    if (eventItem != null)
                    {
                        await ProcessChatEventAsync(eventItem, mediatr, scope, stoppingToken);
                    }
                }
                else if (routingKey == "command.log")
                {
                    var execEvent = JsonSerializer.Deserialize<CommandExecutionEvent>(message, jsonOptions);
                    if (execEvent != null)
                    {
                        // [v2.2] 로그 인리칭: 명령어 로그 처리 시에도 상관관계 ID 유지
                        using (LogContext.PushProperty("CorrelationId", execEvent.CorrelationId))
                        {
                            _logger.LogDebug("📉 [천상의 장부] 명령어 로그 수신: {Keyword}", execEvent.Keyword);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [청취자] 메시지 중사/중계 중 오류 발생 (RoutingKey: {RoutingKey})", routingKey);
            }

            await Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
        
        _logger.LogInformation("✅ [청취자 안착] Topic 구독 활성화 완료. (Queue: {QueueName})", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessChatEventAsync(ChatEventItem item, MediatR.IMediator mediatr, IServiceScope scope, CancellationToken ct)
    {
        // [v2.2] 로그 인리칭: 이 블록 내에서 발생하는 모든 로그에 CorrelationId를 자동으로 삽입
        using (LogContext.PushProperty("CorrelationId", item.MessageId))
        {
            // 1. 패킷 파싱
            using var doc = JsonDocument.Parse(item.JsonPayload);
            var root = doc.RootElement;
            string eventName = root[0].GetString() ?? "";

            // 2. 프로필 조회 (캐시 활용)
            var identityCache = scope.ServiceProvider.GetRequiredService<IIdentityCacheService>();
            var profile = await identityCache.GetStreamerProfileAsync(item.ChzzkUid, ct);
            
            if (profile == null) return;

            if (eventName == "CHAT")
            {
                var payloadString = root[1].GetString() ?? "{}";
                using var payloadDoc = JsonDocument.Parse(payloadString);
                var payload = payloadDoc.RootElement;

                string msg = payload.GetProperty("content").GetString() ?? "";
                string senderId = payload.TryGetProperty("senderChannelId", out var idProp) ? idProp.GetString() ?? "" : "";
                
                var profileJson = payload.GetProperty("profile").ValueKind == JsonValueKind.String
                                        ? payload.GetProperty("profile").GetString() ?? "{}"
                                        : payload.GetProperty("profile").GetRawText();

                using var profileDoc = JsonDocument.Parse(profileJson);
                string nickname = profileDoc.RootElement.TryGetProperty("nickname", out var n) ? n.GetString() ?? "시청자" : "시청자";
                string userRole = profileDoc.RootElement.TryGetProperty("userRoleCode", out var r) ? r.GetString() ?? "common_user" : "common_user";

                JsonElement? emojis = payload.TryGetProperty("emojis", out var e) ? e : null;

                // [v2.2] Reactor 트리거: 원본 MessageId를 CorrelationId로 전달
                await mediatr.Publish(new ChatMessageReceivedEvent(item.MessageId, profile, nickname, msg, userRole, senderId, emojis, 0), ct);
            }
            else if (eventName == "DONATION")
            {
                _logger.LogInformation("💰 [후원 중계] {ChzzkUid} 채널의 후원 이벤트를 Reactor로 전달합니다.", item.ChzzkUid);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
