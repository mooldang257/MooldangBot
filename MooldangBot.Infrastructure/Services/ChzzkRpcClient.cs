using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.ChzzkAPI.Contracts;
using MooldangBot.ChzzkAPI.Contracts.Models.Commands;
using MooldangBot.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 전령 RPC 실구현체]: RabbitMQ RPC 패턴을 사용하여 게이트웨이와 통신합니다.
/// </summary>
public class ChzzkRpcClient : IChzzkRpcClient, IDisposable
{
    private readonly RabbitMQPersistentConnection _connection;
    private readonly ILogger<ChzzkRpcClient> _logger;
    private IChannel? _channel;
    private string? _replyQueueName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private bool _disposed;

    public ChzzkRpcClient(RabbitMQPersistentConnection connection, ILogger<ChzzkRpcClient> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task InitializeAsync()
    {
        if (_channel is { IsOpen: true }) return;

        if (!_connection.IsConnected) await _connection.TryConnectAsync();
        
        _channel = await _connection.CreateModelAsync();
        
        // [v3.7] 전용 응답 큐 선언 (Exclusive: 클라이언트 종료 시 자동 삭제)
        var queueDeclare = await _channel.QueueDeclareAsync(
            queue: "", 
            durable: false, 
            exclusive: true, 
            autoDelete: true);
        
        _replyQueueName = queueDeclare.QueueName;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            if (string.IsNullOrEmpty(correlationId)) return;

            var body = ea.Body.ToArray();
            var responseJson = Encoding.UTF8.GetString(body);

            _logger.LogDebug("📥 [RPC 응답 수신] CorrelationId: {Id}", correlationId);

            if (_pendingRequests.TryRemove(correlationId, out var tcs))
            {
                tcs.TrySetResult(responseJson);
            }

            await Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(_replyQueueName, true, consumer);
        _logger.LogInformation("📡 [RPC 클라이언트] 통신 준비 완료. (ReplyQueue: {Queue})", _replyQueueName);
    }

    public async Task<TResponse> SendCommandAsync<TResponse>(ChzzkCommandBase command, TimeSpan timeout) where TResponse : CommandResponseBase
    {
        await InitializeAsync();

        var correlationId = command.MessageId.ToString();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[correlationId] = tcs;

        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName,
            ContentType = "application/json",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        // v3.7 고성능 직렬화 (Source Generator 사용)
        // [중요] 다형성 식별자(commandType) 포함을 위해 베이스 타입(ChzzkCommandBase)으로 직렬화합니다.
        var json = JsonSerializer.Serialize((object)command, typeof(ChzzkCommandBase), ChzzkJsonContext.Default);
        var body = Encoding.UTF8.GetBytes(json);

        _logger.LogInformation("🚀 [RPC 명령 송신] 유형: {Type}, ID: {Id}", command.GetType().Name, correlationId);

        await _channel!.BasicPublishAsync(
            exchange: RabbitMqExchanges.BotCommands,
            routingKey: "",
            mandatory: false,
            basicProperties: props,
            body: body);

        try
        {
            // 타임아웃 적용 (WaitAsync는 .NET 6+ 지원)
            var responseJson = await tcs.Task.WaitAsync(timeout);
            
            var response = JsonSerializer.Deserialize(responseJson, typeof(TResponse), ChzzkJsonContext.Default) as TResponse;
            return response ?? throw new InvalidOperationException("응답 역직렬화에 실패했습니다.");
        }
        catch (TimeoutException)
        {
            _pendingRequests.TryRemove(correlationId, out _);
            _logger.LogError("⚠️ [RPC 타임아웃] 명령 {Id}에 대한 응답이 {Timeout}초 내에 오지 않았습니다.", correlationId, timeout.TotalSeconds);
            throw;
        }
        catch (Exception ex)
        {
            _pendingRequests.TryRemove(correlationId, out _);
            _logger.LogError(ex, "❌ [RPC 통신 오류] {Id} 처리 중 예외 발생", correlationId);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        foreach (var tcs in _pendingRequests.Values) tcs.TrySetCanceled();
        _channel?.Dispose();
    }
}
