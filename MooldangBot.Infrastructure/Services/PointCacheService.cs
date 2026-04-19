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

    public async Task<IDictionary<string, PointVariant>> ExtractAllIncrementalPointsAsync()
    {
        var db = _redis.GetDatabase();
        var result = new Dictionary<string, PointVariant>();

        // 1. 동기화가 필요한 모든 키 목록을 가져옴
        var pendingKeys = await db.SetMembersAsync(GlobalUpdateSetKey);
        if (pendingKeys.Length == 0) return result;

        // [물멍의 지혜]: Lua 스크립트를 사용하여 포인트와 닉네임을 원자적으로 추출
        const string extractScript = @"
            local p_val = redis.call('GET', @p_key)
            local n_val = redis.call('GET', @n_key)
            if p_val then
                redis.call('DEL', @p_key)
                redis.call('DEL', @n_key)
                return {p_val, n_val}
            else
                return nil
            end";

        var preparedScript = LuaScript.Prepare(extractScript);

        foreach (var redisKey in pendingKeys)
        {
            var keyStr = redisKey.ToString();
            // "viewer_points:streamerUid:viewerUid" -> "viewer_nickname:streamerUid:viewerUid"
            var nickKeyStr = keyStr.Replace(PointKeyPrefix, NicknameKeyPrefix);
            
            var val = await db.ScriptEvaluateAsync(preparedScript, new { 
                p_key = (RedisKey)keyStr, 
                n_key = (RedisKey)nickKeyStr 
            });
            
            if (!val.IsNull)
            {
                var response = (RedisValue[])val;
                var amount = (int)response[0];
                var nickname = response[1].HasValue ? response[1].ToString() : "Unknown";

                // Uid들 추출
                var parts = keyStr.Split(':');
                if (parts.Length >= 3)
                {
                    var mapKey = $"{parts[1]}:{parts[2]}";
                    result[mapKey] = new PointVariant(amount, nickname);
                }
            }
            
            // 처리된 키는 대기 목록에서 제거
            await db.SetRemoveAsync(GlobalUpdateSetKey, redisKey);
        }

        return result;
    }
}
