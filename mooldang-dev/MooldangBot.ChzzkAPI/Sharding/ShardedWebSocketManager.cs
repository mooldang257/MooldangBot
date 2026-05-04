using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using StackExchange.Redis;

namespace MooldangBot.ChzzkAPI.Sharding;

/// <summary>
/// [오시리스의 지혜]: 여러 개의 WebSocket 샤드를 총괄 관리하는 매니저입니다.
/// Application 레이어의 IChzzkChatClient를 구현하여 백그라운드 서비스와 연동됩니다.
/// </summary>
public class ShardedWebSocketManager : IShardedWebSocketManager, IDisposable, IAsyncDisposable
{
    private readonly ILogger<ShardedWebSocketManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MooldangBot.Domain.Contracts.Chzzk.Interfaces.IChzzkApiClient _apiClient;
    private readonly MooldangBot.Domain.Contracts.Chzzk.Interfaces.IChzzkGatewayTokenStore _tokenStore;
    private readonly IConfiguration _configuration;
    private readonly IConnectionMultiplexer _redis;
    private readonly ConcurrentDictionary<int, IWebSocketShard> _shards = new();
    private readonly SemaphoreSlim _initializeSemaphore = new(1, 1);
    private int _shardCount;
    private bool _isInitialized;
    private bool _isDisposed;

    public ShardedWebSocketManager(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        MooldangBot.Domain.Contracts.Chzzk.Interfaces.IChzzkApiClient apiClient,
        MooldangBot.Domain.Contracts.Chzzk.Interfaces.IChzzkGatewayTokenStore tokenStore,
        IConfiguration configuration,
        IConnectionMultiplexer redis)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ShardedWebSocketManager>();
        _scopeFactory = scopeFactory;
        _apiClient = apiClient;
        _tokenStore = tokenStore;
        _configuration = configuration;
        _redis = redis;
    }

    /// <summary>
    /// [오시리스의 시동]: 실제 샤드 초기화 및 실행을 담당합니다.
    /// </summary>
    public async Task StartAsync(int initialShardCount = 1)
    {
        // [시니어 가이드]: 원자적 초기화(InitializeOnce) 패턴을 적용하여 중복 초기화 경쟁 조건을 방지합니다.
        await _initializeSemaphore.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                _logger.LogInformation("ℹ️ [Sharding] 샤드 매니저가 이미 초기화되었습니다. 추가 초기화를 건너뜁니다.");
                return;
            }

            _shardCount = initialShardCount;
            _logger.LogInformation("🚀 [Sharding] 샤드 매니저 초기화를 시작합니다... (목표 샤드 수: {Count})", _shardCount);

            for (int i = 0; i < _shardCount; i++)
            {
                // 각 샤드마다 독립적인 Scope를 가짐 (Publisher 등 주입 목적)
                using var scope = _scopeFactory.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IChzzkMessagePublisher>();
                
                // [v3.7.2] WebSocketShard 생성 시 IConfiguration 명시적 전달
                var shard = new WebSocketShard(
                    shardId: i, 
                    loggerFactory: _loggerFactory, 
                    scopeFactory: _scopeFactory, 
                    publisher: publisher, 
                    apiClient: _apiClient, 
                    tokenStore: _tokenStore,
                    configuration: _configuration,
                    redis: _redis);
                    
                _shards[i] = shard;
            }

            _isInitialized = true;
            _logger.LogInformation("✅ [Sharding] {Count}개의 샤드가 초기화 완료되었습니다.", _shardCount);
        }
        finally
        {
            _initializeSemaphore.Release();
        }
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _initializeSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("🔌 [Sharding] 모든 샤드를 안전하게 종료합니다.");
            foreach (var shard in _shards.Values)
            {
                shard.Dispose();
            }
            _shards.Clear();
            _shardCount = 0;
            _isInitialized = false;
        }
        finally
        {
            _initializeSemaphore.Release();
        }
        await Task.CompletedTask;
    }

    public async Task InitializeAsync() => await StartAsync();

    public bool IsConnected(string chzzkUid)
    {
        chzzkUid = chzzkUid.ToLower();
        if (_shardCount == 0) return false;
        int shardId = Math.Abs(chzzkUid.GetHashCode()) % _shardCount;
        return _shards.TryGetValue(shardId, out var shard) && shard.IsConnected(chzzkUid);
    }

    public bool HasAuthError(string chzzkUid) => false; // 구현 필요 시 확장

    public async Task<bool> ConnectAsync(string chzzkUid, string accessToken, string? clientId = null, string? clientSecret = null)
    {
        chzzkUid = chzzkUid.ToLower();
        try 
        {
            // [오시리스의 영혼]: WebSocket 연결을 위한 서버 URL을 API 클라이언트로부터 획득합니다.
            var status = await _apiClient.GetSessionUrlAsync(chzzkUid, accessToken);
            if (status == null || string.IsNullOrEmpty(status.Url))
            {
                _logger.LogWarning("❌ [Sharding] {ChzzkUid}의 채팅 서버 URL을 찾을 수 없습니다.", chzzkUid);
                return false;
            }

            var url = status.Url; 
            
            if (_shardCount == 0) return false;
            int shardId = Math.Abs(chzzkUid.GetHashCode()) % _shardCount;
            if (_shards.TryGetValue(shardId, out var shard))
            {
                _logger.LogInformation("🛰️ [Sharding] 채널 {ChzzkUid}를 샤드 #{ShardId}에 할당하여 연결합니다.", chzzkUid, shardId);
                await shard.ConnectAsync(chzzkUid, url, accessToken);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Sharding] {ChzzkUid} 연결 시도 중 오류 발생", chzzkUid);
            return false;
        }
    }

    public async Task ConnectAsync(string chzzkUid, string url, string accessToken)
    {
        chzzkUid = chzzkUid.ToLower();
        if (_shardCount == 0) return;
        int shardId = Math.Abs(chzzkUid.GetHashCode()) % _shardCount;
        if (_shards.TryGetValue(shardId, out var shard))
        {
            await shard.ConnectAsync(chzzkUid, url, accessToken);
        }
    }

    public async Task DisconnectAsync(string chzzkUid)
    {
        chzzkUid = chzzkUid.ToLower();
        if (_shardCount == 0) return;
        int shardId = Math.Abs(chzzkUid.GetHashCode()) % _shardCount;
        if (_shards.TryGetValue(shardId, out var shard))
        {
            _logger.LogInformation("🔌 [Sharding] 채널 {ChzzkUid}의 연결 해제를 요청합니다. (샤드 #{ShardId}).", chzzkUid, shardId);
            await shard.DisconnectAsync(chzzkUid);
        }
    }
    
    public int GetActiveConnectionCount() => _shards.Values.Sum(s => s.GetActiveConnectionCount());

    public Task<IEnumerable<ShardStatus>> GetShardStatusesAsync() => Task.FromResult(_shards.Values.Select(s => new ShardStatus(s.ShardId, s.GetActiveConnectionCount(), true)));

    public async Task<bool> SendMessageAsync(string chzzkUid, string message)
    {
        var token = await _tokenStore.GetTokenAsync(chzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return false;
        
        var res = await _apiClient.SendChatMessageAsync(chzzkUid, message, token.AuthCookie); 
        return res != null;
    }

    public async Task<bool> SendNoticeAsync(string chzzkUid, string message)
    {
        var token = await _tokenStore.GetTokenAsync(chzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return false;

        return await _apiClient.SetChatNoticeAsync(chzzkUid, new MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Chat.SetChatNoticeRequest { Message = message }, token.AuthCookie);
    }

    public async Task<bool> UpdateTitleAsync(string chzzkUid, string newTitle)
    {
        var token = await _tokenStore.GetTokenAsync(chzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return false;

        // [v3.1.7] 공식 명세에 따른 DefaultLiveTitle 필드를 사용하여 방제 변경을 수행합니다.
        return await _apiClient.UpdateLiveSettingAsync(chzzkUid, new MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Live.UpdateLiveSettingRequest { DefaultLiveTitle = newTitle }, token.AuthCookie);
    }

    public async Task<bool> UpdateCategoryAsync(string chzzkUid, string category)
    {
        var token = await _tokenStore.GetTokenAsync(chzzkUid);
        if (string.IsNullOrEmpty(token.AuthCookie)) return false;

        // [v2.7.2] 카테고리가 텍스트가 아닌 ID 규격임을 확인하여 업데이트합니다.
        return await _apiClient.UpdateLiveSettingAsync(chzzkUid, new MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Live.UpdateLiveSettingRequest { CategoryId = category }, token.AuthCookie);
    }

    public async Task<bool> InjectEventAsync(string chzzkUid, string eventName, string rawJson)
    {
        try
        {
            if (_shardCount == 0) return false;
            int shardId = Math.Abs(chzzkUid.GetHashCode()) % _shardCount;
            if (_shards.TryGetValue(shardId, out var shard) && shard is WebSocketShard ws)
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
                _logger.LogInformation("🧪 [Simulation] 채널 {ChzzkUid}에 테스트 이벤트({EventName}) 주입 시도 (Shard: {ShardId})", chzzkUid, eventName, shardId);
                await ws.RouteToProcessorAsync(chzzkUid, doc.RootElement, eventName);
                return true;
            }
            return false;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "❌ [Simulation] 이벤트 주입 중 오류 발생: {ChzzkUid}", chzzkUid);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }


    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (var shard in _shards.Values)
        {
            shard.Dispose();
        }
        _shards.Clear();
    }
}
