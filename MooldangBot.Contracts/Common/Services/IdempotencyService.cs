using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MooldangBot.Contracts.Common.Services;

/// <summary>
/// [오시리스의 가드]: Redis SET NX를 활용하여 멱등성을 보장하는 실전 구현체입니다.
/// </summary>
public class IdempotencyService(IConnectionMultiplexer redis, ILogger<IdempotencyService> logger)
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
            bool success = await db.StringSetAsync(redisKey, "processing", expiry, When.NotExists);

            if (!success)
            {
                // [v3.0] Metrics는 Application 레이어 의존성이므로 Contracts에서는 로깅으로 대체하거나 별도 인터페이스 필요
                // FleetMetrics.IdempotencyBlocked.WithLabels("general").Inc();
                logger.LogWarning("⚠️ [중복 요청 감지] 이미 처리 중이거나 완료된 요청입니다: {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            // FleetMetrics.IdempotencyErrors.WithLabels("general").Inc();
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
