using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;

namespace MooldangBot.ChzzkAPI.Sharding;

/// <summary>
/// [오시리스의 지혜]: 여러 개의 WebSocket 샤드를 총괄 관리하는 매니저입니다.
/// </summary>
public class ShardedWebSocketManager : IShardedWebSocketManager, IDisposable
{
    private readonly ILogger<ShardedWebSocketManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChzzkApiClient _apiClient;
    private readonly ConcurrentDictionary<int, IWebSocketShard> _shards = new();
    private readonly int _shardCount;
    private bool _isDisposed;

    public ShardedWebSocketManager(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        IChzzkApiClient apiClient)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ShardedWebSocketManager>();
        _scopeFactory = scopeFactory;
        _apiClient = apiClient;
        
        // [설정]: 인스턴스당 샤드 개수 (기본 10개)
        if (!int.TryParse(config["SHARD_COUNT"], out _shardCount))
        {
            _shardCount = 10;
        }

        InitializeShards();
    }

    private void InitializeShards()
    {
        _logger.LogInformation("📡 [Sharding] {Count}개의 샤드 초기화를 시작합니다.", _shardCount);
        for (int i = 0; i < _shardCount; i++)
        {
            // 각 샤드마다 독립적인 Scope를 가짐 (Publisher 등 주입 목적)
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IChzzkMessagePublisher>();
            
            // [물멍]: Shard 초기화 시 apiClient와 loggerFactory를 올바르게 전달합니다.
            var shard = new WebSocketShard(i, _loggerFactory, _scopeFactory, publisher, _apiClient);
            _shards[i] = shard;
        }
    }

    public async Task ConnectAsync(string chzzkUid, string url, string accessToken)
    {
        // 채널 UID를 해싱하여 특정 샤드에 할당 (부하 분산)
        int shardId = Math.Abs(chzzkUid.GetHashCode()) % _shardCount;
        if (_shards.TryGetValue(shardId, out var shard))
        {
            _logger.LogInformation("🛰️ [Sharding] 채널 {ChzzkUid}를 샤드 #{ShardId}에 할당합니다.", chzzkUid, shardId);
            await shard.ConnectAsync(chzzkUid, url, accessToken);
        }
    }

    public async Task DisconnectAsync(string chzzkUid)
    {
        int shardId = Math.Abs(chzzkUid.GetHashCode()) % _shardCount;
        if (_shards.TryGetValue(shardId, out var shard))
        {
            _logger.LogInformation("🔌 [Sharding] 채널 {ChzzkUid}의 연결 해제를 요청합니다. (샤드 #{ShardId}).", chzzkUid, shardId);
            // WebSocketShard 내부에서 DisconnectAsync 구현 필요 (현재 ConnectAsync만 존재)
        }
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
