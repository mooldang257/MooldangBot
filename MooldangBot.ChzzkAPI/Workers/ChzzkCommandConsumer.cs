using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.ChzzkAPI.Workers;

/// <summary>
/// [아웃바운드 집행자]: RabbitMQ를 통해 Api 서버로부터 명령을 하달받아 실제 소켓에 전달하는 서비스입니다.
/// </summary>
public class ChzzkCommandConsumer : BackgroundService
{
    private readonly ILogger<ChzzkCommandConsumer> _logger;
    private readonly IChzzkChatClient _chzzkChatClient;
    private readonly RabbitMQPersistentConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly string _exchangeName = RabbitMqExchanges.BotCommands;
    private IChannel? _channel;

    public ChzzkCommandConsumer(
        ILogger<ChzzkCommandConsumer> logger,
        IChzzkChatClient chzzkChatClient,
        RabbitMQPersistentConnection connection,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _chzzkChatClient = chzzkChatClient;
        _connection = connection;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_connection.IsConnected) await _connection.TryConnectAsync();

        _channel = await _connection.CreateModelAsync();
        await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Direct, true);

        // [오시리스의 무대]: 인스턴스 전용 임시 큐 생성
        var queueName = (await _channel.QueueDeclareAsync()).QueueName;
        await _channel.QueueBindAsync(queueName, _exchangeName, "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var command = JsonSerializer.Deserialize<ChzzkBotCommand>(message, options);
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
        _logger.LogInformation("📥 [명령 수신] 유형: {Type}, 채널: {ChzzkUid}", command.CommandType, command.ChzzkUid);

        switch (command.CommandType)
        {
            case BotCommandType.SendMessage:
                if (!string.IsNullOrEmpty(command.Payload))
                {
                    bool success = await _chzzkChatClient.SendMessageAsync(command.ChzzkUid, command.Payload);
                    
                    // [시니어 팁]: 만약 연결이 안 되어 있어서 실패했다면, 자동으로 다시 연결 시도 후 재발송 로직을 고려할 수 있습니다.
                    if (!success)
                    {
                        _logger.LogWarning("⚠️ [명령 지연] {ChzzkUid} 소켓 연결이 없거나 담당 인스턴스가 아닙니다. (자동 재연결 대기)", command.ChzzkUid);
                        // SendMessage 실패 시 Reconnect를 유도하거나 직접 호출할 수 있음
                    }
                    else
                    {
                        _logger.LogInformation("✅ [명령 완료] {ChzzkUid} 메시지 전송 성공 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                    }
                }
                break;

            case BotCommandType.Disconnect:
                await _chzzkChatClient.DisconnectAsync(command.ChzzkUid);
                _logger.LogInformation("🔌 [명령 완료] {ChzzkUid} 연결 종료 수행 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                break;

            case BotCommandType.Reconnect:
                _logger.LogInformation("🔄 [명령 실행] {ChzzkUid} 채널에 대한 실시간 연결을 수립합니다...", command.ChzzkUid);
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    var profile = await db.StreamerProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ChzzkUid == command.ChzzkUid);

                    if (profile != null && !string.IsNullOrEmpty(profile.ChzzkAccessToken))
                    {
                        var clientId = _configuration["CHZZK_CLIENT_ID"];
                        var clientSecret = _configuration["CHZZK_CLIENT_SECRET"];

                        bool success = await _chzzkChatClient.ConnectAsync(command.ChzzkUid, profile.ChzzkAccessToken, clientId, clientSecret);
                        if (success) _logger.LogInformation("✅ [연결 성공] {ChzzkUid} 소켓이 가동되었습니다.", command.ChzzkUid);
                        else _logger.LogWarning("⚠️ [연결 거부] {ChzzkUid} 채널 연결이 거부되었습니다 (담당 구역 아님 등).", command.ChzzkUid);
                    }
                    else
                    {
                        _logger.LogError("❌ [연결 실패] {ChzzkUid} 채널의 토큰 정보를 DB에서 찾을 수 없습니다.", command.ChzzkUid);
                    }
                }
                break;

            case BotCommandType.RefreshSettings:
                _logger.LogInformation("⚙️ [명령 완료] {ChzzkUid} 설정 새로고침 수신 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                break;

            case BotCommandType.SendChatNotice:
                if (!string.IsNullOrEmpty(command.Payload))
                {
                    bool success = await _chzzkChatClient.SendNoticeAsync(command.ChzzkUid, command.Payload);
                    if (success) _logger.LogInformation("✅ [공지 완료] {ChzzkUid} 상단 공지 등록 성공 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                    else _logger.LogWarning("⚠️ [공지 실패] {ChzzkUid} 상단 공지 등록 실패 (소켓 미연결 등)", command.ChzzkUid);
                }
                break;

            case BotCommandType.UpdateTitle:
                if (!string.IsNullOrEmpty(command.Payload))
                {
                    bool success = await _chzzkChatClient.UpdateTitleAsync(command.ChzzkUid, command.Payload);
                    if (success) _logger.LogInformation("✅ [방제 완료] {ChzzkUid} 방송 제목 변경 성공 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                    else _logger.LogWarning("⚠️ [방제 실패] {ChzzkUid} 방송 제목 변경 실패", command.ChzzkUid);
                }
                break;

            case BotCommandType.UpdateCategory:
                if (!string.IsNullOrEmpty(command.Payload))
                {
                    bool success = await _chzzkChatClient.UpdateCategoryAsync(command.ChzzkUid, command.Payload);
                    if (success) _logger.LogInformation("✅ [분류 완료] {ChzzkUid} 카테고리 변경 성공 (MsgId: {MsgId})", command.ChzzkUid, command.MessageId);
                    else _logger.LogWarning("⚠️ [분류 실패] {ChzzkUid} 카테고리 변경 실패", command.ChzzkUid);
                }
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
