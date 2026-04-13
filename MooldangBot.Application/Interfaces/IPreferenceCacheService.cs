namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 기억]: 사용자의 전술적 설정(Preference)을 Redis에 임시 보관하고 조회하는 서비스입니다.
/// </summary>
public interface IPreferenceCacheService
{
    /// <summary>
    /// 사용자별 설정을 저장합니다. 기본적으로 TTL(유효기간)을 가집니다.
    /// </summary>
    Task SetPreferenceAsync(string userId, string key, string value, TimeSpan? expiry = null);

    /// <summary>
    /// 사용자별 설정을 조회합니다. 데이터가 없거나 만료된 경우 null을 반환합니다.
    /// </summary>
    Task<string?> GetPreferenceAsync(string userId, string key);
    
    /// <summary>
    /// 특정 설정을 즉시 삭제합니다.
    /// </summary>
    Task RemovePreferenceAsync(string userId, string key);
}
