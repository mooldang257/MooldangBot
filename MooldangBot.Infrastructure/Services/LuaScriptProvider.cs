using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Application.Common.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [심연의 조율자]: Redis Lua 스크립트를 관리하고 실질적으로 실행하는 서비스입니다.
/// </summary>
public class LuaScriptProvider(IConnectionMultiplexer redis, IChaosManager chaosManager, ILogger<LuaScriptProvider> logger) : ILuaScriptProvider
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string KeyPrefix = "roulette:v1:last-end:";
    private const string OverlayKeyPrefix = "overlay:v1:connections:";

    // [v17.0] 룰렛 종료 시각 원자적 동기화 주문
    private const string RouletteSyncScript = @"
        local last = redis.call('get', KEYS[1])
        local now = tonumber(ARGV[1])
        local start = now
        if last then 
            start = math.max(tonumber(last), now) 
        end
        local next_end = start + tonumber(ARGV[2])
        redis.call('setex', KEYS[1], 3600, next_end)
        return next_end
    ";

    // [v17.0] 오버레이 카운트 Underflow 방지 주문
    private const string SafeDecrementScript = @"
        local val = redis.call('get', KEYS[1])
        if val and tonumber(val) > 0 then
            return redis.call('decr', KEYS[1])
        else
            return 0
        end
    ";

    public async Task<long> EvaluateRouletteSyncAsync(string chzzkUid, long nowTicks, long durationTicks)
    {
        // [v18.0] 심연의 시련: 가상 장애 또는 실제 연결 실패 시 폴백
        if (chaosManager.IsRedisPanic) throw new RedisException("🔥 [심연의 시련] 가상 Redis 장애 활성화 중");

        try
        {
            var key = KeyPrefix + chzzkUid.ToLower();
            var result = await _db.ScriptEvaluateAsync(RouletteSyncScript, 
                new RedisKey[] { key }, 
                new RedisValue[] { nowTicks, durationTicks });

            return (long)result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🎰 [Redis Panic] 룰렛 동기화 스크립트 실행 실패. 폴백 모드로 전환합니다.");
            throw; // 상위 State에서 폴백 처리하도록 던짐
        }
    }

    public async Task<long> EvaluateSafeDecrementAsync(string chzzkUid)
    {
        if (chaosManager.IsRedisPanic) return 0;

        try
        {
            var key = OverlayKeyPrefix + chzzkUid.ToLower();
            var result = await _db.ScriptEvaluateAsync(SafeDecrementScript, 
                new RedisKey[] { key });

            return (long)result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "🛡️ [Redis Panic] 오버레이 차감 실패. 로컬 카운터에 의존합니다.");
            return 0;
        }
    }
}
