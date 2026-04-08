using System.Text.Json;
using MooldangBot.Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [영겁의 파수꾼 - 실체]: StackExchange.Redis를 사용하여 토큰을 중앙 집중식으로 관리합니다.
/// </summary>
public class RedisTokenStore : IChzzkTokenStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTokenStore> _logger;
    private const string KeyPrefix = "tokens:";
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisTokenStore(IConnectionMultiplexer redis, ILogger<RedisTokenStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase GetDb() => _redis.GetDatabase();

    public async Task<ChzzkTokenInfo?> GetTokenAsync(string chzzkUid)
    {
        var db = GetDb();
        var data = await db.StringGetAsync($"{KeyPrefix}{chzzkUid}");

        if (data.IsNullOrEmpty) return null;

        try
        {
            return JsonSerializer.Deserialize<ChzzkTokenInfo>(data!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [토큰 저장소] {ChzzkUid}의 토큰 역직렬화 중 오류 발생", chzzkUid);
            return null;
        }
    }

    public async Task SetTokenAsync(string chzzkUid, ChzzkTokenInfo tokenInfo)
    {
        var db = GetDb();
        var json = JsonSerializer.Serialize(tokenInfo, _jsonOptions);
        
        // [이지스 전략]: 토큰 수명보다 1시간 더 길게 캐시하여 안정성 확보
        var expiry = (tokenInfo.ExpiresAt - DateTime.UtcNow).Add(TimeSpan.FromHours(1));
        if (expiry < TimeSpan.Zero) expiry = TimeSpan.FromHours(24); // Fallback

        await db.StringSetAsync($"{KeyPrefix}{chzzkUid}", json, expiry);
        _logger.LogDebug("📡 [토큰 저장소] {ChzzkUid}의 토큰을 Redis에 갱신했습니다.", chzzkUid);
    }

    public async Task RemoveTokenAsync(string chzzkUid)
    {
        var db = GetDb();
        await db.KeyDeleteAsync($"{KeyPrefix}{chzzkUid}");
    }
}
