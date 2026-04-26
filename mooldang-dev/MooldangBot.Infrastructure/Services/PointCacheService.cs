using MooldangBot.Modules.Point.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v7.0] Redis 기반 포인트 캐시 구현체: Lua 스크립트를 사용하여 원자적 포인트 추출 및 
/// 데이터 유실 없는 Write-Back 기반을 제공합니다.
/// </summary>
public class PointCacheService : IPointCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<PointCacheService> _logger;
    private const string PointKeyPrefix = "viewer_points:";
    
    // [세피로스의 지리]: 모든 채널의 변동분 키 목록을 관리하는 Set 키
    private const string NicknameKeyPrefix = "viewer_nickname:";
    
    // [세피로스의 지리]: 모든 채널의 변동분 키 목록을 관리하는 Set 키
    private const string GlobalUpdateSetKey = "viewer_points_pending_sync";

    // [오시리스의 기록부]: 2-Phase Commit을 위한 스냅샷 접두사
    private const string SnapshotPrefix = "viewer_points_snapshot:";
    private const string ProcessingSetPrefix = "viewer_points_processing:";

    public PointCacheService(IConnectionMultiplexer redis, ILogger<PointCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task AddPointAsync(string streamerUid, string viewerUid, string nickname, int amount)
    {
        var db = _redis.GetDatabase();
        var key = $"{PointKeyPrefix}{streamerUid.ToLower()}:{viewerUid.ToLower()}";
        var nickKey = $"{NicknameKeyPrefix}{streamerUid.ToLower()}:{viewerUid.ToLower()}";
        
        // 1. 포인트 누적 (Atomic INCR)
        await db.StringIncrementAsync(key, amount);
        
        // 2. 닉네임 최신화 (덮어쓰기)
        await db.StringSetAsync(nickKey, nickname);
        
        // 3. 동기화 대기 목록에 추가 (Set 자료구조를 사용하여 중복 방지)
        await db.SetAddAsync(GlobalUpdateSetKey, key);
    }

    public async Task<int> GetIncrementalPointAsync(string streamerUid, string viewerUid)
    {
        var db = _redis.GetDatabase();
        var key = $"{PointKeyPrefix}{streamerUid.ToLower()}:{viewerUid.ToLower()}";
        var val = await db.StringGetAsync(key);
        return val.HasValue ? (int)val : 0;
    }

    public async Task<string?> CreateSyncSnapshotAsync()
    {
        var db = _redis.GetDatabase();
        
        // 1. 대기 목록 존재 확인
        if (!await db.KeyExistsAsync(GlobalUpdateSetKey)) return null;

        var snapshotId = Guid.NewGuid().ToString("n");
        var processingKey = $"{ProcessingSetPrefix}{snapshotId}";
        var snapshotKey = $"{SnapshotPrefix}{snapshotId}";

        // 2. 대기 목록을 처리 목록으로 원자적 이동 (Rename)
        try 
        {
            await db.KeyRenameAsync(GlobalUpdateSetKey, processingKey);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("no such key"))
        {
            return null; // 그 사이에 다른 워커가 가져감
        }

        // 3. 처리 목록의 키들을 스냅샷 해시에 백업하고 원본 키 삭제 (Atomic Snapshot)
        var pendingKeys = await db.SetMembersAsync(processingKey);
        const string backupScript = @"
            local p_val = redis.call('GET', @p_key)
            local n_val = redis.call('GET', @n_key)
            if p_val then
                redis.call('HSET', @snapshot_key, @p_key, p_val)
                redis.call('HSET', @snapshot_key, @n_key, n_val)
                redis.call('DEL', @p_key)
                redis.call('DEL', @n_key)
                return 1
            end
            return 0";
        var preparedBackup = LuaScript.Prepare(backupScript);

        foreach (var key in pendingKeys)
        {
            var keyStr = key.ToString();
            var nickKeyStr = keyStr.Replace(PointKeyPrefix, NicknameKeyPrefix);
            await db.ScriptEvaluateAsync(preparedBackup, new { 
                p_key = (RedisKey)keyStr, 
                n_key = (RedisKey)nickKeyStr,
                snapshot_key = (RedisKey)snapshotKey
            });
        }

        return snapshotId;
    }

    public async Task<IDictionary<string, PointVariant>> GetSnapshotDataAsync(string snapshotId)
    {
        var db = _redis.GetDatabase();
        var snapshotKey = $"{SnapshotPrefix}{snapshotId}";
        var result = new Dictionary<string, PointVariant>();

        var allEntries = await db.HashGetAllAsync(snapshotKey);
        var entryDict = allEntries.ToDictionary(x => x.Name.ToString(), x => x.Value);

        // 해시 데이터를 PointVariant 맵으로 변환
        foreach (var entry in entryDict)
        {
            var keyStr = entry.Key;
            if (!keyStr.StartsWith(PointKeyPrefix)) continue;

            var amount = (int)entry.Value;
            var nickKey = keyStr.Replace(PointKeyPrefix, NicknameKeyPrefix);
            var nickname = entryDict.TryGetValue(nickKey, out var nVal) ? nVal.ToString() : "Unknown";

            var parts = keyStr.Split(':');
            if (parts.Length >= 3)
            {
                var mapKey = $"{parts[1]}:{parts[2]}";
                result[mapKey] = new PointVariant(amount, nickname);
            }
        }

        return result;
    }

    public async Task RemoveSnapshotAsync(string snapshotId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"{SnapshotPrefix}{snapshotId}");
        await db.KeyDeleteAsync($"{ProcessingSetPrefix}{snapshotId}");
    }

    public async Task<IEnumerable<string>> GetOrphanedSnapshotsAsync()
    {
        // Redis 서버에서 직접 패턴 검색 (서버 부하 주의, 실무에서는 별도 인덱싱 권장)
        var server = _redis.GetServer(_redis.GetEndPoints()[0]);
        var keys = server.Keys(pattern: $"{ProcessingSetPrefix}*");
        return keys.Select(k => k.ToString().Replace(ProcessingSetPrefix, ""));
    }

    public async Task<IDictionary<string, PointVariant>> ExtractAllIncrementalPointsAsync()
    {
        // 하위 호환성을 위해 스냅샷 로직을 사용하여 구현
        var snapshotId = await CreateSyncSnapshotAsync();
        if (string.IsNullOrEmpty(snapshotId)) return new Dictionary<string, PointVariant>();

        var data = await GetSnapshotDataAsync(snapshotId);
        await RemoveSnapshotAsync(snapshotId);
        return data;
    }
}
