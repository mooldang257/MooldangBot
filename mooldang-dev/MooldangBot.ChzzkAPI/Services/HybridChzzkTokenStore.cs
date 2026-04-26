using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using StackExchange.Redis;

namespace MooldangBot.ChzzkAPI.Services;

/// <summary>
/// [영겁의 파수꾼 - 하이브리드]: Redis와 DB(MariaDB)를 결합하여 안정적인 토큰 조회를 보장합니다.
/// Redis에 정보가 없을 경우 DB에서 복구하여 정지 없는 서비스를 유지합니다.
/// </summary>
public class HybridChzzkTokenStore : IChzzkGatewayTokenStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HybridChzzkTokenStore> _logger;
    private const string KeyPrefix = "tokens:";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HybridChzzkTokenStore(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<HybridChzzkTokenStore> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private IDatabase GetDb() => _redis.GetDatabase();

    public async Task<(string SessionCookie, string AuthCookie)> GetTokenAsync(string chzzkUid)
    {
        // 1. Redis에서 먼저 시도
        var redisDb = GetDb();
        var data = await redisDb.StringGetAsync($"{KeyPrefix}{chzzkUid}");

        if (!data.IsNullOrEmpty)
        {
            try
            {
                var info = JsonSerializer.Deserialize<ChzzkTokenInfo>((string)data!, _jsonOptions);
                if (info != null && !string.IsNullOrEmpty(info.AccessToken))
                {
                    return (string.Empty, info.AccessToken); // Gateway에서는 AccessToken을 AuthCookie로 사용
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [HybridTokenStore] Redis 토큰 역직렬화 실패: {ChzzkUid}", chzzkUid);
            }
        }

        // 2. Redis에 없거나 오류 발생 시 DB에서 복구 시도
        _logger.LogInformation("🔄 [HybridTokenStore] Redis에 {ChzzkUid} 정보 없음. DB에서 복구를 시도합니다...", chzzkUid);
        
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var streamer = await dbContext.CoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);

        if (streamer != null && !string.IsNullOrEmpty(streamer.ChzzkAccessToken))
        {
            _logger.LogInformation("✅ [HybridTokenStore] DB에서 {ChzzkUid}의 토큰을 찾았습니다. Redis를 갱신합니다.", chzzkUid);
            
            // Redis 자동 복구 (Self-Healing)
            var expiry = streamer.TokenExpiresAt.HasValue 
                ? (streamer.TokenExpiresAt.Value.Value - DateTime.UtcNow).Add(TimeSpan.FromHours(1))
                : TimeSpan.FromHours(24);
            
            if (expiry < TimeSpan.Zero) expiry = TimeSpan.FromHours(2); // 이미 만료된 경우라도 최소 유지

            var info = new ChzzkTokenInfo(
                streamer.ChzzkAccessToken,
                streamer.ChzzkRefreshToken ?? string.Empty,
                streamer.TokenExpiresAt?.Value ?? DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow);

            await SetRedisAsync(chzzkUid, info, expiry);

            return (string.Empty, streamer.ChzzkAccessToken);
        }

        _logger.LogWarning("⚠️ [HybridTokenStore] DB에도 {ChzzkUid}의 토큰 정보가 존재하지 않습니다.", chzzkUid);
        return (string.Empty, string.Empty);
    }

    public async Task SetTokenAsync(string chzzkUid, string sessionCookie, string authCookie)
    {
        // Gateway에서 직접 Set하는 경우 (보통은 Presentation에서 처리하지만, 호환성을 위해 유지)
        var info = new ChzzkTokenInfo(authCookie, string.Empty, DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        await SetRedisAsync(chzzkUid, info, TimeSpan.FromHours(2));
    }

    public async Task<IDictionary<string, (string SessionCookie, string AuthCookie)>> GetAllTokensAsync()
    {
        // 모니터링용 전체 조회 (DB 기준)
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var streamers = await dbContext.CoreStreamerProfiles
            .AsNoTracking()
            .Where(s => !string.IsNullOrEmpty(s.ChzzkAccessToken))
            .ToListAsync();

        return streamers.ToDictionary(
            s => s.ChzzkUid, 
            s => (string.Empty, s.ChzzkAccessToken!)
        );
    }

    private async Task SetRedisAsync(string chzzkUid, ChzzkTokenInfo info, TimeSpan expiry)
    {
        try
        {
            var db = GetDb();
            var json = JsonSerializer.Serialize(info, _jsonOptions);
            await db.StringSetAsync($"{KeyPrefix}{chzzkUid}", json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [HybridTokenStore] Redis 쓰기 실패: {ChzzkUid}", chzzkUid);
        }
    }
}
