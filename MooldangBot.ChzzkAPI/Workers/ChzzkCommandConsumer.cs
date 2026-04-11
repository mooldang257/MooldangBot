using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Internal;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MooldangBot.ChzzkAPI.Workers;

/// <summary>
/// [오시리스의 수신부]: RabbitMQ를 통해 전달되는 게이트웨이 제어 명령을 처리합니다.
/// </summary>
public class ChzzkCommandConsumer : BackgroundService
{
    private readonly ILogger<ChzzkCommandConsumer> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IShardedWebSocketManager _shardManager;
    private readonly IChzzkApiClient _apiClient;
    private readonly IChzzkTokenStore _tokenStore;
    private IConnection? _connection;
    private IChannel? _channel;

    public ChzzkCommandConsumer(
        ILogger<ChzzkCommandConsumer> logger,
        IConnectionFactory connectionFactory,
        IShardedWebSocketManager shardManager,
        IChzzkApiClient apiClient,
        IChzzkTokenStore tokenStore)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _shardManager = shardManager;
        _apiClient = apiClient;
        _tokenStore = tokenStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(queue: "chzzk.commands", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            _logger.LogInformation("📡 [CommandConsumer] RabbitMQ 명령 수신 대기가 시작되었습니다. (Queue: chzzk.commands)");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("📥 [CommandConsumer] 명령 수신: {Message}", message);
                
                try
                {
                    var command = JsonSerializer.Deserialize<ChzzkBotCommand>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (command != null)
                    {
                        await ProcessCommandAsync(command);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [CommandConsumer] 명령 처리 중 오류 발생");
                }
                
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            };

            await _channel.BasicConsumeAsync(queue: "chzzk.commands", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [CommandConsumer] RabbitMQ 연결 중 오류가 발생했습니다.");
        }
    }

    private async Task ProcessCommandAsync(ChzzkBotCommand command)
    {
        _logger.LogInformation("🚀 [명령 집행] 유형: {Type}, 채널: {ChzzkUid}", command.CommandType, command.ChzzkUid);

        switch (command.CommandType)
        {
            case BotCommandType.Reconnect:
                await HandleReconnectAsync(command.ChzzkUid);
                break;

            case BotCommandType.Disconnect:
                await _shardManager.DisconnectAsync(command.ChzzkUid);
                break;

            case BotCommandType.SendMessage:
                if (!string.IsNullOrEmpty(command.Payload))
                {
                    var token = await _tokenStore.GetTokenAsync(command.ChzzkUid);
                    if (!string.IsNullOrEmpty(token.AuthCookie))
                    {
                        await _apiClient.SendChatMessageAsync(command.ChzzkUid, command.Payload, token.AuthCookie);
                    }
                }
                break;

            default:
                _logger.LogWarning("❓ [명령 무시] 처리할 수 없는 명령어 유형: {Type}", command.CommandType);
                break;
        }
    }

    private async Task HandleReconnectAsync(string chzzkUid)
    {
        try
        {
            var token = await _tokenStore.GetTokenAsync(chzzkUid);
            if (string.IsNullOrEmpty(token.AuthCookie))
            {
                _logger.LogWarning("⚠️ [연결 실패] {ChzzkUid}의 토큰 정보가 없습니다.", chzzkUid);
                return;
            }

            var sessionResponse = await _apiClient.GetSessionUrlAsync(chzzkUid, token.AuthCookie);
            if (sessionResponse != null && !string.IsNullOrEmpty(sessionResponse.Url))
            {
                await _shardManager.ConnectAsync(chzzkUid, sessionResponse.Url, token.AuthCookie);
                _logger.LogInformation("✅ [연결 성공] {ChzzkUid} 채널이 샤드에 할당되었습니다.", chzzkUid);
            }
            else
            {
                _logger.LogError("❌ [연결 실패] {ChzzkUid}의 세션 URL을 획득하지 못했습니다.", chzzkUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [재연결 오류] {ChzzkUid} 처리 중 예외 발생", chzzkUid);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
