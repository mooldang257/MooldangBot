using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MooldangBot.ChzzkAPI.Workers;

/// <summary>
/// [아웃바운드 집행자]: RabbitMQ를 통해 Api 서버로부터 명령을 하달받아 실제 소켓에 전달하는 서비스입니다.
/// </summary>
public class ChzzkCommandConsumer : BackgroundService
{
    private readonly ILogger<ChzzkCommandConsumer> _logger;
    private readonly IChzzkChatClient _chzzkChatClient;
    private readonly RabbitMQPersistentConnection _connection;
    private readonly string _exchangeName = RabbitMqExchanges.BotCommands;
    private IChannel? _channel;

    public ChzzkCommandConsumer(
        ILogger<ChzzkCommandConsumer> logger,
        IChzzkChatClient chzzkChatClient,
        RabbitMQPersistentConnection connection)
    {
        _logger = logger;
        _chzzkChatClient = chzzkChatClient;
        _connection = connection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_connection.IsConnected) await _connection.TryConnectAsync();

        _channel = await _connection.CreateModelAsync();
        await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Direct, true);

        // [오시리스의 무대]: 인스턴스 전용 임시 큐 생성 (Fanout 대신 Direct를 사용하여 특정 명령 하달 가능성 열어둠)
        // 현재는 모든 인스턴스가 명령 익스체인지를 바라보되, ShardedWebSocketManager가 자신의 책임 여부를 판별함.
        var queueName = (await _channel.QueueDeclareAsync()).QueueName;
        await _channel.QueueBindAsync(queueName, _exchangeName, "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var command = JsonSerializer.Deserialize<ChzzkBotCommand>(message);
                if (command != null)
                {
                    await ProcessCommandAsync(command);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [명령 집행자] 명령 처리 중 오류 발생: {Message}", ex.Message);
            }

            await Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
        
        _logger.LogInformation("📡 [명령 집행자] 아웃바운드 명령 수신 대기 중... (Queue: {QueueName})", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessCommandAsync(ChzzkBotCommand command)
    {
        // [v2.0] 수평 확장을 고려하여, 이 인스턴스가 해당 스트리머를 담당하고 있는지 확인
        // ShardedWebSocketManager 내부에 이미 이 로직이 포함되어 있거나, 직접 판단 가능
        
        _logger.LogInformation("📥 [명령 수신] 유형: {Type}, 채널: {ChzzkUid}", command.CommandType, command.ChzzkUid);

        switch (command.CommandType)
        {
            case BotCommandType.SendMessage:
                if (!string.IsNullOrEmpty(command.Payload))
                {
                    bool success = await _chzzkChatClient.SendMessageAsync(command.ChzzkUid, command.Payload);
                    if (success) _logger.LogInformation("✅ [명령 완료] {ChzzkUid} 메시지 전송 성공 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                    else _logger.LogWarning("⚠️ [명령 실패] {ChzzkUid} 메시지 전송 실패 (담당 샤드가 아니거나 연결 끊김) (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                }
                break;

            case BotCommandType.Disconnect:
                await _chzzkChatClient.DisconnectAsync(command.ChzzkUid);
                _logger.LogInformation("🔌 [명령 완료] {ChzzkUid} 연결 종료 수행 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                break;

            case BotCommandType.Reconnect:
                _logger.LogInformation("🔄 [명령 완료] {ChzzkUid} 채널 재연결 명령 수신 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                // TODO: 재연결 로직 구현
                break;

            case BotCommandType.RefreshSettings:
                _logger.LogInformation("⚙️ [명령 완료] {ChzzkUid} 설정 새로고침 수신 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                break;

            default:
                _logger.LogWarning("❓ [명령 수신] 정의되지 않은 명령어 유형입니다: {Type} (MsgId: {MsgId})", command.CommandType, command.MessageId);
                break;
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
