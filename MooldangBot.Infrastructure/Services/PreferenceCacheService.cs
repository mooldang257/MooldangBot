using MooldangBot.Contracts.Common.Interfaces;
using StackExchange.Redis;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 영속]: Redis를 활용한 사용자별 전술 설정 기억 장치입니다.
/// </summary>
public class PreferenceCacheService(IConnectionMultiplexer redis) : IPreferenceCacheService
{
    private const string Prefix = "pref";

    // [물멍]: 선장님 지시 사항 - pref:{userId}:{key} 규칙 준수
    private static string GetKey(string userId, string key) => $"{Prefix}:{userId}:{key}";

    public async Task SetPreferenceAsync(string userId, string key, string value, TimeSpan? expiry = null)
    {
        try 
        {
            var db = redis.GetDatabase();
            // [물멍]: 일부 환경의 라이브러리 버전 차이(Expiration 타입 이슈)를 고려하여 분리 사격
            await db.StringSetAsync(GetKey(userId, key), value);
            if (expiry.HasValue)
            {
                await db.KeyExpireAsync(GetKey(userId, key), expiry.Value);
            }
        }
        catch (Exception ex)
        {
            // [Fallback]: 함교 운용은 계속되어야 하므로 에러만 기록
            Console.WriteLine($"[물멍의 경보] Redis Preference 저장 실패 (UserId: {userId}, Key: {key}): {ex.Message}");
        }
    }

    public async Task<string?> GetPreferenceAsync(string userId, string key)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(GetKey(userId, key));
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[물멍의 경보] Redis Preference 조회 실패 (Key: {GetKey(userId, key)}): {ex.Message}");
            return null;
        }
    }

    public async Task RemovePreferenceAsync(string userId, string key)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(GetKey(userId, key));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[물멍의 경보] Redis Preference 삭제 실패 (Key: {GetKey(userId, key)}): {ex.Message}");
        }
    }
}
