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
    
    // [오시리스의 기억]: GC 압박을 줄이기 위한 직렬화 옵션 캐싱
    private static readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false 
    };

    static RabbitMqService()
    {
        _jsonOptions.TypeInfoResolverChain.Insert(0, MooldangBot.ChzzkAPI.Serialization.ChzzkJsonContext.Default);
        _jsonOptions.TypeInfoResolverChain.Add(new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
    }

    // [전령의 통로]: 채널 재사용을 위한 필드 및 정합성 수호자
    private IChannel? _channel;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private bool _disposed;

    public bool IsConnected => _connection.IsConnected;

    public RabbitMqService(RabbitMQPersistentConnection connection, ILogger<RabbitMqService> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _channelLock.WaitAsync();
        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            if (!_connection.IsConnected) await _connection.TryConnectAsync();
            
            _channel = await _connection.CreateModelAsync();

            // [오시리스의 선언]: 채널 생성 시 익스체인지를 한 번만 선언 (중복 네트워크 호출 제거)
            await _channel.ExchangeDeclareAsync(ChatExchange, ExchangeType.Fanout, true);
            await _channel.ExchangeDeclareAsync(LogExchange, ExchangeType.Topic, true);

            _logger.LogInformation("📡 [전령] RabbitMQ 전용 채널이 개설되고 익스체인지가 선언되었습니다.");
            return _channel;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public async Task<bool> CheckConnectionAsync() => await _connection.TryConnectAsync();

    public async Task PublishChatEventAsync(ChatEventItem eventItem)
    {
        await PublishInternalAsync(eventItem, "chat.event", ChatExchange);
    }

    public async Task PublishAsync<T>(T eventData, string? routingKey = null) where T : class
    {
        await PublishInternalAsync(eventData, routingKey ?? typeof(T).Name.ToLower(), LogExchange);
    }

    private async Task PublishInternalAsync<T>(T eventData, string routingKey, string exchangeName) where T : class
    {
        try
        {
            var channel = await GetChannelAsync();
            var json = JsonSerializer.Serialize(eventData, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);
            
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channelLock.WaitAsync(); // 채널 동시 접근 보호 (비동기 송신)
            try
            {
                await channel.BasicPublishAsync(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);
            }
            finally
            {
                _channelLock.Release();
            }

            _logger.LogDebug("📡 [전령] 이벤트 발행 완료 : {RoutingKey} (Ex: {Exchange})", routingKey, exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [전령] 이벤트 발행 중 오류 발생: {Message}", ex.Message);
            _channel = null; // 오류 발생 시 채널 재발급 유도
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _channel?.Dispose();
        _channelLock.Dispose();
    }
}
