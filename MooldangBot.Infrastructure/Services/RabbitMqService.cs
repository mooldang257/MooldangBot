using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 전령]: RabbitMQ.Client 7.x를 사용하는 IRabbitMqService의 실전 구현체입니다.
/// RabbitMQPersistentConnection을 통해 중단 없는 메시징 인프라를 제공합니다.
/// </summary>
public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly RabbitMQPersistentConnection _connection;
    private readonly ILogger<RabbitMqService> _logger;
    private const string ChatExchange = "mooldang.chat.events";
    private const string LogExchange = "mooldang.bot.events";
    private bool _disposed;

    public bool IsConnected => _connection.IsConnected;

    public RabbitMqService(RabbitMQPersistentConnection connection, ILogger<RabbitMqService> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CheckConnectionAsync()
    {
        return await _connection.TryConnectAsync();
    }

    public async Task PublishChatEventAsync(ChatEventItem eventItem)
    {
        // [Legacy Support] 기존 채팅 이벤트 발행 로직 (Fanout 기반)
        await PublishAsync(eventItem, "chat.event", ChatExchange, ExchangeType.Fanout);
    }

    public async Task PublishAsync<T>(T eventData, string? routingKey = null) where T : class
    {
        // [New Standard] 비즈니스 로직 및 로그 발행 (Topic 기반)
        await PublishInternalAsync(eventData, routingKey ?? typeof(T).Name.ToLower(), LogExchange, ExchangeType.Topic);
    }

    // 하위 호환성을 위해 routingKey가 없는 오버로드 (특정 익스체인지 지정용)
    private async Task PublishAsync<T>(T eventData, string routingKey, string exchangeName, string type) where T : class
    {
        await PublishInternalAsync(eventData, routingKey, exchangeName, type);
    }

    private async Task PublishInternalAsync<T>(T eventData, string routingKey, string exchangeName, string type) where T : class
    {
        try
        {
            if (!_connection.IsConnected)
            {
                await _connection.TryConnectAsync();
            }

            using var channel = await _connection.CreateModelAsync();

            // [오시리스의 선언]: 익스체인지가 없으면 신규 선언
            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: type,
                durable: true,
                autoDelete: false);

            var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false 
            });
            
            var body = Encoding.UTF8.GetBytes(json);
            
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            // [전령의 배달]: 비동기 메시지 발행
            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogDebug($"📡 [전령] 이벤트 발행 완료 : {routingKey} (Ex: {exchangeName})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [전령] 이벤트 발행 중 오류 발생: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
    }
}
