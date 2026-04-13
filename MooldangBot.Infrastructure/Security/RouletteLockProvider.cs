using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Interfaces;
using RedLockNet;
using StackExchange.Redis;

namespace MooldangBot.Infrastructure.Security;

/// <summary>
/// [오시리스의 수호자]: Redis 분산 락(RedLock)과 로컬 세마포어(Panic Fallback)를 결합한 하이브리드 락 제공자입니다.
/// (Aegis of Hardening): Redis가 죽더라도 로컬 환경에서 정합성을 유지하며 항해를 계속합니다.
/// </summary>
public class RouletteLockProvider : IRouletteLockProvider, IDisposable
{
    private readonly IDistributedLockFactory _lockFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly IChaosManager _chaos;
    private readonly ILogger<RouletteLockProvider> _logger;

    // [오시리스의 정원]: 좀비 세마포어를 방지하기 위한 시간 관리형 저장소
    private static readonly ConcurrentDictionary<string, LockEntry> _localLocks = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public RouletteLockProvider(
        IDistributedLockFactory lockFactory, 
        IConnectionMultiplexer redis,
        IChaosManager chaos,
        ILogger<RouletteLockProvider> logger)
    {
        _lockFactory = lockFactory;
        _redis = redis;
        _chaos = chaos;
        _logger = logger;
        
        // [v12.0] 1분마다 사용되지 않는 락을 수거하는 파수꾼 가동
        _cleanupTimer = new Timer(CleanupZombieLocks, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<IDisposable?> AcquireLockAsync(string chzzkUid, TimeSpan wait, TimeSpan expiry)
    {
        await _chaos.TryInjectFaultAsync("RouletteLock");

        var lockKey = $"lock:roulette:{chzzkUid.ToLower()}";

        if (_redis.IsConnected)
        {
            try
            {
                var redLock = await _lockFactory.CreateLockAsync(lockKey, expiry, wait, TimeSpan.FromSeconds(1));
                if (redLock.IsAcquired) return redLock;

                _logger.LogTrace("[오시리스의 거절] 룰렛 락 획득 실패 (이미 실행 중): {ChzzkUid}", chzzkUid);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔥 [오시리스의 떨림] Redis 분산 락 시도 중 오류 발생. 패닉 폴백으로 전환합니다.");
            }
        }

        // [오시리스의 보호]: 리소스 누수 방지형 로컬 락 획득
        var entry = _localLocks.GetOrAdd(chzzkUid.ToLower(), _ => new LockEntry());
        entry.LastAccessedAt = DateTime.UtcNow; // 접근 시간 갱신

        bool acquired = await entry.Semaphore.WaitAsync(wait);
        if (acquired)
        {
            return new LocalLockReleaser(entry);
        }

        return null;
    }

    private void CleanupZombieLocks(object? state)
    {
        var now = DateTime.UtcNow;
        var gracePeriod = TimeSpan.FromMinutes(10); // [물멍의 제안]: 10분의 유예 기간

        foreach (var kvp in _localLocks)
        {
            // 사용 중이 아니고(CurrentCount == 1), 유예 기간이 지났다면 제거
            if (kvp.Value.Semaphore.CurrentCount == 1 && (now - kvp.Value.LastAccessedAt) > gracePeriod)
            {
                if (_localLocks.TryRemove(kvp.Key, out var entry))
                {
                    entry.Semaphore.Dispose();
                    _logger.LogDebug("🧹 [오시리스의 청소] 장기간 사용되지 않은 로컬 락을 수거했습니다: {Key}", kvp.Key);
                }
            }
        }
    }

    private class LockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    }

    private class LocalLockReleaser(LockEntry entry) : IDisposable
    {
        public void Dispose()
        {
            entry.LastAccessedAt = DateTime.UtcNow; // 반납 시에도 시간 갱신
            entry.Semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cleanupTimer.Dispose();
    }
}
