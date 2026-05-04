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
    private const string PointKeyPrefix = "FuncViewerPoints:";
    
    // [세피로스의 지리]: 모든 채널의 변동분 키 목록을 관리하는 Set 키
    private const string NicknameKeyPrefix = "ViewerNickname:";
    
    // [세피로스의 지리]: 모든 채널의 변동분 키 목록을 관리하는 Set 키
    private const string GlobalUpdateSetKey = "ViewerPointsPendingSync";

    // [오시리스의 기록부]: 2-Phase Commit을 위한 스냅샷 접두사
    private const string SnapshotPrefix = "ViewerPointsSnapshot:";
    private const string ProcessingSetPrefix = "ViewerPointsProcessing:";

    public PointCacheService(IConnectionMultiplexer redis, ILogger<PointCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task AddPointAsync(string StreamerUid, string ViewerUid, string Nickname, int Amount)
    {
        var Db = _redis.GetDatabase();
        var Key = $"{PointKeyPrefix}{StreamerUid.ToLower()}:{ViewerUid.ToLower()}";
        var NickKey = $"{NicknameKeyPrefix}{StreamerUid.ToLower()}:{ViewerUid.ToLower()}";
        
        // 1. 포인트 누적 (Atomic INCR)
        await Db.StringIncrementAsync(Key, Amount);
        
        // 2. 닉네임 최신화 (덮어쓰기)
        await Db.StringSetAsync(NickKey, Nickname);
        
        // 3. 동기화 대기 목록에 추가 (Set 자료구조를 사용하여 중복 방지)
        await Db.SetAddAsync(GlobalUpdateSetKey, Key);
    }

    public async Task<int> GetIncrementalPointAsync(string StreamerUid, string ViewerUid)
    {
        var Db = _redis.GetDatabase();
        var Key = $"{PointKeyPrefix}{StreamerUid.ToLower()}:{ViewerUid.ToLower()}";
        var Val = await Db.StringGetAsync(Key);
        return Val.HasValue ? (int)Val : 0;
    }

    public async Task<string?> CreateSyncSnapshotAsync()
    {
        var Db = _redis.GetDatabase();
        
        // 1. 대기 목록 존재 확인
        if (!await Db.KeyExistsAsync(GlobalUpdateSetKey)) return null;
 
        var SnapshotId = Guid.NewGuid().ToString("n");
        var ProcessingKey = $"{ProcessingSetPrefix}{SnapshotId}";
        var SnapshotKey = $"{SnapshotPrefix}{SnapshotId}";
 
        // 2. 대기 목록을 처리 목록으로 원자적 이동 (Rename)
        try 
        {
            await Db.KeyRenameAsync(GlobalUpdateSetKey, ProcessingKey);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("no such key"))
        {
            return null; // 그 사이에 다른 워커가 가져감
        }
 
        // 3. 처리 목록의 키들을 스냅샷 해시에 백업하고 원본 키 삭제 (Atomic Snapshot)
        var PendingKeys = await Db.SetMembersAsync(ProcessingKey);
        const string backupScript = @"
            local P_Val = redis.call('GET', @P_Key)
            local N_Val = redis.call('GET', @N_Key)
            if P_Val then
                redis.call('HSET', @Snapshot_Key, @P_Key, P_Val)
                redis.call('HSET', @Snapshot_Key, @N_Key, N_Val)
                redis.call('DEL', @P_Key)
                redis.call('DEL', @N_Key)
                return 1
            end
            return 0";
        var PreparedBackup = LuaScript.Prepare(backupScript);

        foreach (var Key in PendingKeys)
        {
            var KeyStr = Key.ToString();
            var NickKeyStr = KeyStr.Replace(PointKeyPrefix, NicknameKeyPrefix);
            await Db.ScriptEvaluateAsync(PreparedBackup, new { 
                P_Key = (RedisKey)KeyStr, 
                N_Key = (RedisKey)NickKeyStr,
                Snapshot_Key = (RedisKey)SnapshotKey
            });
        }

        return SnapshotId;
    }

    public async Task<IDictionary<string, PointVariant>> GetSnapshotDataAsync(string SnapshotId)
    {
        var Db = _redis.GetDatabase();
        var SnapshotKey = $"{SnapshotPrefix}{SnapshotId}";
        var Result = new Dictionary<string, PointVariant>();

        var AllEntries = await Db.HashGetAllAsync(SnapshotKey);
        var EntryDict = AllEntries.ToDictionary(x => x.Name.ToString(), x => x.Value);

        // 해시 데이터를 PointVariant 맵으로 변환
        foreach (var Entry in EntryDict)
        {
            var KeyStr = Entry.Key;
            if (!KeyStr.StartsWith(PointKeyPrefix)) continue;

            var Amount = (int)Entry.Value;
            var NickKey = KeyStr.Replace(PointKeyPrefix, NicknameKeyPrefix);
            var Nickname = EntryDict.TryGetValue(NickKey, out var NVal) ? NVal.ToString() : "Unknown";

            var Parts = KeyStr.Split(':');
            if (Parts.Length >= 3)
            {
                var MapKey = $"{Parts[1]}:{Parts[2]}";
                Result[MapKey] = new PointVariant(Amount, Nickname);
            }
        }

        return Result;
    }

    public async Task RemoveSnapshotAsync(string SnapshotId)
    {
        var Db = _redis.GetDatabase();
        await Db.KeyDeleteAsync($"{SnapshotPrefix}{SnapshotId}");
        await Db.KeyDeleteAsync($"{ProcessingSetPrefix}{SnapshotId}");
    }

    public async Task<IEnumerable<string>> GetOrphanedSnapshotsAsync()
    {
        // Redis 서버에서 직접 패턴 검색 (서버 부하 주의, 실무에서는 별도 인덱싱 권장)
        var Server = _redis.GetServer(_redis.GetEndPoints()[0]);
        var Keys = Server.Keys(pattern: $"{ProcessingSetPrefix}*");
        return Keys.Select(k => k.ToString().Replace(ProcessingSetPrefix, ""));
    }

    public async Task<IDictionary<string, PointVariant>> ExtractAllIncrementalPointsAsync()
    {
        // 하위 호환성을 위해 스냅샷 로직을 사용하여 구현
        var SnapshotId = await CreateSyncSnapshotAsync();
        if (string.IsNullOrEmpty(SnapshotId)) return new Dictionary<string, PointVariant>();
 
        var Data = await GetSnapshotDataAsync(SnapshotId);
        await RemoveSnapshotAsync(SnapshotId);
        return Data;
    }
}
