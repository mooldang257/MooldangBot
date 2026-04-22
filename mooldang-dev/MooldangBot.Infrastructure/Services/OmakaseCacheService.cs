using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using StackExchange.Redis;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [서기의 캐시 실구현]: Redis를 사용하여 오마카세 메뉴 주문 횟수와 메타데이터를 관리합니다.
/// (v15.2): 고부하 환경에서의 원자적 카운트 보장을 위해 IConnectionMultiplexer를 직접 사용합니다.
/// </summary>
public class OmakaseCacheService : IOmakaseCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<OmakaseCacheService> _logger;
    private const string OmakaseCountKeyPrefix = "omakase:count:";
    private const string OmakaseIconKeyPrefix = "omakase:icon:";

    public OmakaseCacheService(IConnectionMultiplexer redis, ILogger<OmakaseCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<int> GetCountAsync(int streamerProfileId, int menuId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"{OmakaseCountKeyPrefix}{menuId}";
        var val = await db.StringGetAsync(key);
        return val.HasValue ? (int)val : 0;
    }

    public async Task<int> IncrementCountAsync(int streamerProfileId, int menuId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"{OmakaseCountKeyPrefix}{menuId}";
        var newVal = await db.StringIncrementAsync(key);
        return (int)newVal;
    }

    public async Task<string> GetIconAsync(int streamerProfileId, int menuId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"{OmakaseIconKeyPrefix}{menuId}";
        var val = await db.StringGetAsync(key);
        return val.HasValue ? val.ToString() : "🍣";
    }

    public async Task SyncFromDbAsync(int streamerProfileId, int menuId, string icon, int count, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var countKey = $"{OmakaseCountKeyPrefix}{menuId}";
        var iconKey = $"{OmakaseIconKeyPrefix}{menuId}";

        // 정보가 바뀌었을 때만 업데이트 (혹은 강제 덮어쓰기)
        await db.StringSetAsync(countKey, count);
        await db.StringSetAsync(iconKey, icon);
        
        _logger.LogDebug("🍣 [오마카세 캐시 동기화] MenuId: {MenuId}, Count: {Count}, Icon: {Icon}", menuId, count, icon);
    }
}
