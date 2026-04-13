using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Live;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;
using MooldangBot.Contracts.Integrations.Chzzk;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MooldangBot.ChzzkAPI.Workers;

/// <summary>
/// [오시리스의 수신부 RPC]: RabbitMQ RPC 패턴을 통해 전달되는 게이트웨이 제어 명령을 처리하고 결과를 회신합니다.
/// </summary>
public class CommandRpcWorker : BackgroundService
{
    private readonly ILogger<CommandRpcWorker> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IShardedWebSocketManager _shardManager;
    private readonly IChzzkApiClient _apiClient;
    private readonly IChzzkGatewayTokenStore _tokenStore;
    private IConnection? _connection;
    private IChannel? _channel;

    public CommandRpcWorker(
        ILogger<CommandRpcWorker> logger,
        IConnectionFactory connectionFactory,
        IShardedWebSocketManager shardManager,
        IChzzkApiClient apiClient,
        IChzzkGatewayTokenStore tokenStore)
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
            
            // [오시리스의 선언]: 명령용 익스체인지 및 큐 선언 (v3.7)
            const string exchangeName = "mooldang.chzzk.commands";
            const string queueName = "chzzk.commands";

            await _channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: "", cancellationToken: stoppingToken);

            _logger.LogInformation("📡 [RPC 워커] RabbitMQ 명령 수신 대기 시작 (ID: {Queue})", queueName);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var props = ea.BasicProperties;
                
                _logger.LogInformation("📥 [RPC 명령 수신] ID: {Id}, Type: {Type}", props.CorrelationId, props.Type);

                StandardCommandResponse response;
                try
                {
                    // v3.7 다형성 역직렬화
                    var command = JsonSerializer.Deserialize(message, typeof(ChzzkCommandBase), ChzzkJsonContext.Default) as ChzzkCommandBase;
                    
                    if (command != null)
                    {
                        var (success, error) = await ProcessCommandInternalAsync(command);
                        response = new StandardCommandResponse(
                            CorrelationId: Guid.Parse(props.CorrelationId ?? Guid.Empty.ToString()),
                            IsSuccess: success,
                            ErrorMessage: error,
                            ProcessedAt: DateTimeOffset.UtcNow
                        );
                    }
                    else
                    {
                        response = new StandardCommandResponse(
                            CorrelationId: Guid.Parse(props.CorrelationId ?? Guid.Empty.ToString()),
                            IsSuccess: false,
                            ErrorMessage: "명령어 해석 실패 (알 수 없는 타입)",
                            ProcessedAt: DateTimeOffset.UtcNow
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [RPC 워커] 명령 처리 중 예외 발생");
                    response = new StandardCommandResponse(
                        CorrelationId: Guid.Parse(props.CorrelationId ?? Guid.Empty.ToString()),
                        IsSuccess: false,
                        ErrorMessage: ex.Message,
                        ProcessedAt: DateTimeOffset.UtcNow
                    );
                }

                // [회신] ReplyTo 큐로 결과 전송
                if (!string.IsNullOrEmpty(props.ReplyTo))
                {
                    var responseJson = JsonSerializer.Serialize(response, typeof(StandardCommandResponse), ChzzkJsonContext.Default);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    
                    var replyProps = new BasicProperties { CorrelationId = props.CorrelationId };
                    
                    await _channel.BasicPublishAsync(
                        exchange: "", 
                        routingKey: props.ReplyTo, 
                        mandatory: false, 
                        basicProperties: replyProps, 
                        body: responseBytes);
                        
                    _logger.LogDebug("📤 [RPC 응답 회신] To: {Queue}, CorrelationId: {Id}", props.ReplyTo, props.CorrelationId);
                }

                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            };

            await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // [오시리스의 은신]: 서비스 종료 시 발생하는 정상적인 취소 신호입니다.
            _logger.LogInformation("👋 [RPC 워커] 서비스 종료 신호를 수신하여 안전하게 중단합니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [RPC 워커] RabbitMQ 연결 오류");
        }
    }

    private async Task<(bool Success, string? Error)> ProcessCommandInternalAsync(ChzzkCommandBase command)
    {
        _logger.LogInformation("🚀 [명령 집행] 유형: {Type}, 채널: {ChzzkUid}", command.GetType().Name, command.ChzzkUid);

        return command switch
        {
            SendMessageCommand c => await HandleSendMessageAsync(c),
            SendChatNoticeCommand c => await HandleSendNoticeAsync(c),
            UpdateTitleCommand c => await HandleUpdateTitleAsync(c),
            UpdateCategoryCommand c => await HandleUpdateCategoryAsync(c),
            ReconnectCommand c => await HandleReconnectAsync(c.ChzzkUid),
            DisconnectCommand c => await HandleDisconnectAsync(c.ChzzkUid),
            RefreshSettingsCommand c => await HandleRefreshSettingsAsync(c.ChzzkUid),
            _ => (false, "처리되지 않은 명령어 유형입니다.")
        };
    }

    private async Task<(bool Success, string? Error)> HandleSendMessageAsync(SendMessageCommand c)
    {
        if (string.IsNullOrEmpty(c.Message)) return (false, "메시지 내용이 비어있습니다.");
        
        var token = await _tokenStore.GetTokenAsync(c.ChzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return (false, "인증 정보가 없습니다.");

        await _apiClient.SendChatMessageAsync(c.ChzzkUid, c.Message, token.AuthCookie);
        return (true, null);
    }

    private async Task<(bool Success, string? Error)> HandleSendNoticeAsync(SendChatNoticeCommand c)
    {
        if (string.IsNullOrEmpty(c.Notice)) return (false, "공지 내용이 비어있습니다.");

        var token = await _tokenStore.GetTokenAsync(c.ChzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return (false, "인증 정보가 없습니다.");

        await _apiClient.SetChatNoticeAsync(c.ChzzkUid, new SetChatNoticeRequest { Message = c.Notice }, token.AuthCookie);
        return (true, null);
    }

    private async Task<(bool Success, string? Error)> HandleUpdateTitleAsync(UpdateTitleCommand c)
    {
        if (string.IsNullOrEmpty(c.NewTitle)) return (false, "새 제목이 비어있습니다.");

        var token = await _tokenStore.GetTokenAsync(c.ChzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return (false, "인증 정보가 없습니다.");

        await _apiClient.UpdateLiveSettingAsync(c.ChzzkUid, new UpdateLiveSettingRequest { DefaultLiveTitle = c.NewTitle }, token.AuthCookie);
        return (true, null);
    }

    private async Task<(bool Success, string? Error)> HandleUpdateCategoryAsync(UpdateCategoryCommand c)
    {
        var token = await _tokenStore.GetTokenAsync(c.ChzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return (false, "인증 정보가 없습니다.");

        var categoryId = c.CategoryId;
        var categoryType = c.CategoryType;

        // [v3.1.6] 지휘관님 지침: 카테고리 ID가 없으면 게이트웨이에서 직접 수색
        if (string.IsNullOrEmpty(categoryId) && !string.IsNullOrEmpty(c.SearchKeyword))
        {
            _logger.LogInformation("🔍 [카테고리 수색] Keyword: {Keyword}", c.SearchKeyword);
            var searchRes = await _apiClient.SearchCategoryAsync(c.SearchKeyword);
            var firstResult = searchRes?.Data?.FirstOrDefault();
            
            if (firstResult != null)
            {
                categoryId = firstResult.CategoryId;
                categoryType = firstResult.CategoryType;
                _logger.LogInformation("🎯 [수색 성공] 발견: {Value} ({Id})", firstResult.CategoryValue, categoryId);
            }
            else
            {
                return (false, $"'{c.SearchKeyword}'에 해당하는 카테고리를 찾지 못했습니다.");
            }
        }

        if (string.IsNullOrEmpty(categoryId)) return (false, "카테고리 ID를 확정할 수 없습니다.");

        await _apiClient.UpdateLiveSettingAsync(c.ChzzkUid, new UpdateLiveSettingRequest 
        { 
            CategoryId = categoryId,
            CategoryType = categoryType
        }, token.AuthCookie);
        
        return (true, null);
    }

    private async Task<(bool Success, string? Error)> HandleReconnectAsync(string chzzkUid)
    {
        try
        {
            var token = await _tokenStore.GetTokenAsync(chzzkUid);
            if (string.IsNullOrEmpty(token.AuthCookie)) return (false, "인증 정보가 없습니다.");

            var sessionResponse = await _apiClient.GetSessionUrlAsync(chzzkUid, token.AuthCookie);
            if (sessionResponse != null && !string.IsNullOrEmpty(sessionResponse.Url))
            {
                await _shardManager.ConnectAsync(chzzkUid, sessionResponse.Url, token.AuthCookie);
                return (true, null);
            }
            return (false, "세션 URL 획득 실패");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool Success, string? Error)> HandleDisconnectAsync(string chzzkUid)
    {
        await _shardManager.DisconnectAsync(chzzkUid);
        return (true, null);
    }

    private async Task<(bool Success, string? Error)> HandleRefreshSettingsAsync(string chzzkUid)
    {
        _logger.LogWarning("🚨 [자가 치유] {ChzzkUid} 채널의 설정을 새로고침하고 재연결을 시도합니다.", chzzkUid);
        // [v3.7] 기존 RefreshSettings 로직: 연결 해제 후 재연결 시도
        await _shardManager.DisconnectAsync(chzzkUid);
        return await HandleReconnectAsync(chzzkUid);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
