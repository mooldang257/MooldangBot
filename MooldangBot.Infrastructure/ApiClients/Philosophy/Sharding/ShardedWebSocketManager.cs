using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Standart.Hash.xxHash;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using RedLockNet;
using StackExchange.Redis;
using System.Threading;

namespace MooldangBot.Infrastructure.ApiClients.Philosophy.Sharding;

/// <summary>
/// [파동의 지휘자]: 부하 분산을 위해 여러 개의 WebSocketShard를 관리하는 총괄 매니저입니다.
/// </summary>
public class ShardedWebSocketManager : IChzzkChatClient, IDisposable
{
    private readonly IWebSocketShard[] _shards;
    private readonly int _shardCount;
    private readonly ILogger<ShardedWebSocketManager> _logger;
    private readonly IDistributedLockFactory _lockFactory;
    private readonly IConnectionMultiplexer _redis;
    
    private int _instanceIndex = -1; // [v4.5.0] 초기값 -1 (미할당)
    private readonly int _instanceCount;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private CancellationTokenSource? _heartbeatCts;

    public ShardedWebSocketManager(
        ILoggerFactory loggerFactory, 
        IServiceScopeFactory scopeFactory, 
        IChatEventChannel eventChannel,
        IConfiguration config,
        IDistributedLockFactory lockFactory,
        IConnectionMultiplexer redis,
        int shardCount = 10)
    {
        _logger = loggerFactory.CreateLogger<ShardedWebSocketManager>();
        _lockFactory = lockFactory;
        _redis = redis;
        
        // [v4.0.0] 분산 환경 환경 변수 로드
        string? envIndex = config["SHARD_INDEX"];
        if (!string.IsNullOrEmpty(envIndex) && int.TryParse(envIndex, out var idx))
        {
            _instanceIndex = idx;
        }

        _instanceCount = int.TryParse(config["SHARD_COUNT"], out var count) ? count : 4; 
        _shardCount = shardCount;
        
        _shards = new IWebSocketShard[_shardCount];
        
        for (int i = 0; i < _shardCount; i++)
        {
            _shards[i] = new WebSocketShard(i, loggerFactory, scopeFactory, eventChannel);
        }
    }

    public async Task InitializeAsync()
    {
        if (_instanceIndex >= 0)
        {
            _logger.LogInformation("[파동의 지휘자] 고정 인덱스 {InstanceIndex}/{InstanceCount}로 가동합니다.", _instanceIndex, _instanceCount);
            return;
        }

        _logger.LogInformation("[파동의 탐색] 가용한 인덱스를 자동으로 검색합니다...");

        try
        {
            var db = _redis.GetDatabase();
            for (int i = 0; i < _instanceCount; i++)
            {
                var lockKey = $"shard:registry:{i}";
                // [오시리스의 선점]: 60초간 인덱스 점유 시도
                if (await db.LockTakeAsync(lockKey, _instanceId, TimeSpan.FromSeconds(60)))
                {
                    _instanceIndex = i;
                    _logger.LogInformation("[파동의 정착] 인덱스 {InstanceIndex}를 점유했습니다. (InstanceId: {InstanceId})", _instanceIndex, _instanceId);
                    
                    // 하트비트 시작
                    StartHeartbeat(i);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[오시리스의 탄식] Redis 연결 실패로 인해 자동 인덱스 할당을 수행할 수 없습니다. 로컬 모드(Index: 0)로 강제 전환합니다.");
            _instanceIndex = 0;
            return;
        }

        throw new InvalidOperationException("[파동의 고립] 가용한 SHARD_INDEX를 찾을 수 없습니다. 인스턴스 수를 확인하세요.");
    }

    private void StartHeartbeat(int index)
    {
        _heartbeatCts = new CancellationTokenSource();
        var token = _heartbeatCts.Token;
        var lockKey = $"shard:registry:{index}";

        _ = Task.Run(async () =>
        {
            var db = _redis.GetDatabase();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // [오시리스의 재생]: TTL을 넉넉히 연장 (3배수 법칙 적용)
                    var expiry = TimeSpan.FromSeconds(30); 
                    if (!await db.LockExtendAsync(lockKey, _instanceId, expiry))
                    {
                        _logger.LogWarning("[파동의 위기] 인덱스 {Index}의 점유권 갱신 실패. 재점유를 시도합니다.", index);
                        if (await db.LockTakeAsync(lockKey, _instanceId, expiry))
                        {
                            _logger.LogInformation("[파동의 재탈환] 인덱스 {Index}를 성공적으로 재점유했습니다.", index);
                        }
                        else
                        {
                            _logger.LogCritical("[파동의 절단] 인덱스 {Index}를 영영 잃었습니다. 시스템 안정성을 위해 연결을 중단할 수 있습니다.", index);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[파동의 떨림] 하트비트 갱신 중 오류 발생 (Index: {Index})", index);
                }

                // [오시리스의 휴식]: 10초 대기를 통해 CPU 및 Redis 부하 방지 (Critical Fix)
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }, token);
    }

    private uint GetDeterministicHashCode(string input)
    {
        // [v4.1.0] 결정론적 해싱: 프로세스 재시작 시에도 동일한 해시값 보장
        var bytes = Encoding.UTF8.GetBytes(input);
        return xxHash32.ComputeHash(bytes, bytes.Length);
    }

    private bool IsMyResponsibility(string chzzkUid)
    {
        // [v4.1.0] 물멍 분산 알고리즘: XxHash32 해시를 인스턴스 수로 나누어 담당 여부 판단
        uint hash = GetDeterministicHashCode(chzzkUid);
        return (hash % (uint)_instanceCount) == (uint)_instanceIndex;
    }

    private IWebSocketShard GetShard(string chzzkUid)
    {
        uint hash = GetDeterministicHashCode(chzzkUid);
        uint internalIndex = hash % (uint)_shardCount;
        return _shards[internalIndex];
    }

    public bool IsConnected(string chzzkUid)
    {
        return GetShard(chzzkUid).IsConnected(chzzkUid);
    }

    public async Task<bool> ConnectAsync(string chzzkUid, string accessToken)
    {
        if (!IsMyResponsibility(chzzkUid))
        {
            _logger.LogDebug("[파동의 거부] {ChzzkUid}는 이 인스턴스({InstanceIndex}) 담당이 아닙니다. (무시)", chzzkUid, _instanceIndex);
            return false;
        }

        // [v4.2.0] RedLock 도입: 인스턴스 간 중복 연결 방지
        var resource = $"lock:chat:{chzzkUid}";
        var expiry = TimeSpan.FromSeconds(30);
        var wait = TimeSpan.FromSeconds(10);
        var retry = TimeSpan.FromSeconds(1);

        using (var redLock = await _lockFactory.CreateLockAsync(resource, expiry, wait, retry))
        {
            if (!redLock.IsAcquired)
            {
                _logger.LogWarning("[RedLock 결핍] {ChzzkUid}에 대한 분산 락 획득 실패. 이미 다른 인스턴스에서 점유 중일 수 있습니다.", chzzkUid);
                return false;
            }

            return await GetShard(chzzkUid).ConnectAsync(chzzkUid, accessToken);
        }
    }

    public async Task DisconnectAsync(string chzzkUid)
    {
        await GetShard(chzzkUid).DisconnectAsync(chzzkUid);
    }

    public int GetActiveConnectionCount()
    {
        return _shards.Sum(s => s.ConnectionCount);
    }

    public IEnumerable<ShardStatus> GetShardStatuses()
    {
        return _shards.Select(s => s.GetStatus());
    }

    public async ValueTask DisposeAsync()
    {
        // [오시리스의 회귀]: 비동기 자원 해제 실장 (Wait() 제거)
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();

        if (_instanceIndex >= 0)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.LockReleaseAsync($"shard:registry:{_instanceIndex}", _instanceId);
                _logger.LogInformation("[파동의 귀환] 인덱스 {InstanceIndex} 점유를 해제했습니다.", _instanceIndex);
            }
            catch { }
        }

        foreach (var shard in _shards)
        {
            if (shard is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (shard is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
