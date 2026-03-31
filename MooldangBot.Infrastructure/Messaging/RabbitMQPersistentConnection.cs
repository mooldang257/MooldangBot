using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Events;
using Polly;
using Polly.Retry;

namespace MooldangBot.Infrastructure.Messaging;

/// <summary>
/// [오시리스의 영속]: RabbitMQ와의 안정적인 영속 연결을 관리하는 서비스입니다.
/// RabbitMQ.Client 7.x의 비동기 전용 API와 Polly의 비동기 재시도 정책을 결합하여 가용성을 보장합니다.
/// </summary>
public class RabbitMQPersistentConnection : IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQPersistentConnection> _logger;
    private readonly int _retryCount;
    private IConnection? _connection;
    private bool _disposed;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public RabbitMQPersistentConnection(
        IConnectionFactory connectionFactory, 
        ILogger<RabbitMQPersistentConnection> logger, 
        int retryCount = 5)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
    }

    public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

    public async Task<IChannel> CreateModelAsync()
    {
        if (!IsConnected)
        {
            await TryConnectAsync();
        }

        if (!IsConnected || _connection == null)
        {
            throw new InvalidOperationException("RabbitMQ 연결을 생성할 수 없습니다.");
        }

        return await _connection.CreateChannelAsync();
    }

    public async Task<bool> TryConnectAsync()
    {
        if (_disposed) return false; // [세피로스의 방패]: 이미 파괴된 객체이므로 연결 시도를 전행하지 않습니다.
        if (IsConnected) return true;

        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected) return true;

            _logger.LogInformation("[RabbitMQ] 비동기 연결 시도 중...");

            // [v7.0] Polly 비동기 재시도 전략 적용
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, $"[RabbitMQ] 연결 실패. {time.TotalSeconds:n1}초 후 재시도합니다 ({ex.Message})");
                });

            await policy.ExecuteAsync(async () =>
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
            });

            if (IsConnected && _connection != null)
            {
                _connection.ConnectionShutdownAsync += OnConnectionShutdown;
                _connection.CallbackExceptionAsync += OnCallbackException;
                _connection.ConnectionBlockedAsync += OnConnectionBlocked;

                _logger.LogInformation($"✅ [RabbitMQ] 연결 성공: {_connection.Endpoint.HostName}");
                return true;
            }

            _logger.LogCritical("❌ [RabbitMQ] 모든 재시도 후에도 연결에 실패했습니다.");
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("[RabbitMQ] 연결 차단됨. 재연결을 시도합니다...");
        await TryConnectAsync();
    }

    private async Task OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("[RabbitMQ] 콜백 예외 발생. 재연결을 시도합니다...");
        await TryConnectAsync();
    }

    private async Task OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;
        _logger.LogWarning("[RabbitMQ] 연결 셧다운됨. 재연결을 시도합니다...");
        await TryConnectAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _connection?.Dispose();
            _connectionLock.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }
}
