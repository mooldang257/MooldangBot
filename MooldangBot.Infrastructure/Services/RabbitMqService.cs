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
/// v2.0 분산 아키텍처를 위해 Topic 익스체인지 및 고성능 메시지 전송을 지원합니다.
/// </summary>
public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly RabbitMQPersistentConnection _connection;
    private readonly ILogger<RabbitMqService> _logger;
    
    // [오시리스의 기억]: GC 압박을 줄이기 위한 직렬화 옵션 캐싱
    private static readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false 
    };

    static RabbitMqService()
    {
        // Chzzk 전용 소스 생성 컨텍스트 및 기본 리졸버 체인 구성
        _jsonOptions.TypeInfoResolverChain.Insert(0, MooldangBot.ChzzkAPI.Serialization.ChzzkJsonContext.Default);
        _jsonOptions.TypeInfoResolverChain.Add(new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
    }

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

            // [오시리스의 선언]: 채널 생성 시 필수 익스체인지들을 선언
            await _channel.ExchangeDeclareAsync(RabbitMqExchanges.ChatEvents, ExchangeType.Topic, true);
            await _channel.ExchangeDeclareAsync(RabbitMqExchanges.BotCommands, ExchangeType.Direct, true);
            await _channel.ExchangeDeclareAsync(RabbitMqExchanges.LegacyChat, ExchangeType.Fanout, true);
            await _channel.ExchangeDeclareAsync(RabbitMqExchanges.SystemLogs, ExchangeType.Topic, true);

            _logger.LogInformation("📡 [전령] RabbitMQ v2.0 전용 채널이 개설되고 모든 익스체인지가 선언되었습니다.");
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
        // 하위 호환성 유지 (Legacy Fanout 익스체인지 및 Topic 익스체인지 동시 발행 권장)
        await PublishInternalAsync(eventItem, "chat.event", RabbitMqExchanges.LegacyChat);
        
        // v2.0 전용 라우팅 키: streamer.{chzzkUid}.chat
        await PublishInternalAsync(eventItem, $"streamer.{eventItem.ChzzkUid}.chat", RabbitMqExchanges.ChatEvents);
    }

    public async Task PublishAsync<T>(T eventData, string? routingKey = null, string? exchangeName = null) where T : class
    {
        var exchange = exchangeName ?? RabbitMqExchanges.SystemLogs;
        var rKey = routingKey ?? typeof(T).Name.ToLower();
        
        await PublishInternalAsync(eventData, rKey, exchange);
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

            await _channelLock.WaitAsync(); 
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
            _logger.LogError(ex, "❌ [전령] 이벤트 발행 중 오류 발생 (Ex: {Exchange}): {Message}", exchangeName, ex.Message);
            _channel = null; 
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
