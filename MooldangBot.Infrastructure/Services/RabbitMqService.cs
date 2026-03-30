using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using RabbitMQ.Client;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 전령]: RabbitMQ.Client 7.x를 사용하여 비동기 메시징을 수행합니다.
/// </summary>
public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly ILogger<RabbitMqService> _logger;
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _exchangeName = "mooldang.chat.events";
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public bool IsConnected => _connection != null && _connection.IsOpen;

    public async Task<bool> CheckConnectionAsync()
    {
        await EnsureInitializedAsync();
        return IsConnected;
    }

    public RabbitMqService(IConfiguration config, ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        
        var host = config["RABBITMQ_HOST"] ?? "localhost";
        var port = int.TryParse(config["RABBITMQ_PORT"], out var p) ? p : 5672;
        var user = config["RABBITMQ_USER"] ?? "guest";
        var pass = config["RABBITMQ_PASS"] ?? "guest";

        _factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass
        };
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            _logger.LogInformation("[전령의 연결] RabbitMQ 서버에 접속을 시도합니다...");
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // [오시리스의 선언]: Fanout 익스체인지 생성 (모든 구독자에게 전송)
            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName, 
                type: ExchangeType.Fanout, 
                durable: true);

            _isInitialized = true;
            _logger.LogInformation("[전령의 안착] RabbitMQ 초기화가 완료되었습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[전령의 좌절] RabbitMQ 초기화 중 오류가 발생했습니다.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishChatEventAsync(ChatEventItem eventItem)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_channel == null) return;

            var json = JsonSerializer.Serialize(eventItem);
            var body = Encoding.UTF8.GetBytes(json);

            // [오시리스의 전송]: 메시지 발행
            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: string.Empty,
                body: body);

            _logger.LogDebug($"[전령의 배달] 채팅 이벤트 발행 완료 (Channel: {eventItem.ChzzkUid})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[전령의 분실] 채팅 이벤트 발행 중 오류가 발생했습니다.");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _initLock.Dispose();
    }
}
