using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Chat;
using RabbitMQ.Client;
using Polly;
using System.Net.Sockets;
using RabbitMQ.Client.Exceptions;

namespace MooldangBot.ChzzkAPI.Messaging;

/// <summary>
/// [오버시리스] ChzzkAPI 전용 RabbitMQ 메시지 발행 서비스입니다.
/// RabbitMQ.Client v7.x 규격에 최적화된 비동기 발행을 지원합니다.
/// </summary>
public class RabbitMqChzzkMessagePublisher : IChzzkMessagePublisher, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqChzzkMessagePublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public RabbitMqChzzkMessagePublisher(IConnectionFactory connectionFactory, ILogger<RabbitMqChzzkMessagePublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _lock.WaitAsync();
        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            if (_connection == null || !_connection.IsOpen)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

                await policy.ExecuteAsync(async () =>
                {
                    _connection = await _connectionFactory.CreateConnectionAsync();
                });
            }

            _channel = await _connection!.CreateChannelAsync();
            
            // Exchange 선언
            await _channel.ExchangeDeclareAsync("mooldang.chzzk.chat", ExchangeType.Topic, true);
            await _channel.ExchangeDeclareAsync("mooldang.legacy.chat", ExchangeType.Fanout, true);
            await _channel.ExchangeDeclareAsync("mooldang.chzzk.status", ExchangeType.Topic, true);

            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PublishChatEventAsync(object chatEvent)
    {
        try
        {
            var channel = await GetChannelAsync();
            var json = JsonSerializer.Serialize(chatEvent);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            // RabbitMQ v7.x 비동기 발행 규격 준수
            await channel.BasicPublishAsync(
                exchange: "mooldang.chzzk.chat",
                routingKey: "chzzk.chat.event",
                mandatory: false,
                basicProperties: props,
                body: (ReadOnlyMemory<byte>)body,
                cancellationToken: CancellationToken.None);

            _logger.LogDebug("[Publisher] 채팅 이벤트 발행 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Publisher] 채팅 이벤트 발행 중 오류 발생");
            _channel = null;
        }
    }

    public async Task PublishStatusEventAsync(string chzzkUid, string status)
    {
        try
        {
            var channel = await GetChannelAsync();
            var payload = new { ChzzkUid = chzzkUid, Status = status, Timestamp = DateTime.UtcNow };
            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            // 규격에 맞게 빈 속성(BasicProperties) 생성
            var props = new BasicProperties();

            await channel.BasicPublishAsync(
                exchange: "mooldang.chzzk.status",
                routingKey: $"streamer.{chzzkUid}.status",
                mandatory: false,
                basicProperties: props,
                body: (ReadOnlyMemory<byte>)body,
                cancellationToken: CancellationToken.None);

            _logger.LogInformation("[Publisher] 채널 {ChzzkUid} 상태 변경 발행: {Status}", chzzkUid, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Publisher] 상태 이벤트 발행 중 오류 발생");
            _channel = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _channel?.Dispose();
        _connection?.Dispose();
        _lock.Dispose();
    }
}
