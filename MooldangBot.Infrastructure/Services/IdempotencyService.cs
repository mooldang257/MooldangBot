using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Metrics;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 가드]: Redis SET NX를 활용하여 멱등성을 보장하는 실전 구현체입니다.
/// </summary>
public class IdempotencyService(IConnectionMultiplexer redis, ILogger<IdempotencyService> logger) : IIdempotencyService
{
    private const string Prefix = "idempotency";

    private string GetKey(string key) => $"{Prefix}:{key}";

    public async Task<bool> TryAcquireAsync(string key, TimeSpan expiry)
    {
        try
        {
            var db = redis.GetDatabase();
            var redisKey = GetKey(key);

            // [물멍]: SET key value NX EX expiry
            // NX: Key가 없을 때만 성공, EX: 만료 시간 설정
            // [v2.4] 중복 처리를 원천 봉쇄하는 가장 강력하고 가벼운 방법입니다.
            bool success = await db.StringSetAsync(redisKey, "processing", expiry, When.NotExists);

            if (!success)
            {
                FleetMetrics.IdempotencyBlocked.WithLabels("general").Inc(); // [v2.4.1] 멱등성 가드 실적 카운팅
                logger.LogWarning("⚠️ [중복 요청 감지] 이미 처리 중이거나 완료된 요청입니다: {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            FleetMetrics.IdempotencyErrors.WithLabels("general").Inc(); // [v2.4.1] Fail-Closed 발생 카운팅
            // [오시리스의 침묵 - Fail-Closed 전략]: 
            // 사용자님의 조언에 따라 금융(포인트) 정합성을 위해 Redis 장애 시 요청을 차단합니다.
            // "중복 처리가 발생하는 것보다 서비스가 잠시 멈추는 것이 신뢰를 지키는 길이다."
            logger.LogError(ex, "🚨 [Redis 장애] 멱등성 체크 불가. 데이터 무결성을 위해 요청을 차단(Fail-Closed)합니다. Key: {Key}", key);
            return false; 
        }
    }

    public async Task MarkAsCompletedAsync(string key, TimeSpan expiry)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync(GetKey(key), "completed", expiry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🔥 [Redis 장애] 멱등성 완료 기록 실패: {Key}", key);
        }
    }
}
