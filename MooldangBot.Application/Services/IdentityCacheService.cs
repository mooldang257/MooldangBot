using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Contracts.Security;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Services;

/// <summary>
/// [이지스 실구현체]: 스트리머/시청자 데이터를 IDistributedCache 기반으로 관리합니다.
/// (S2: 확장성): 현재는 로컬 메모리를 사용하지만, 추후 Redis 등 외부 분산 캐시로 즉시 전환 가능합니다.
/// </summary>
public class IdentityCacheService(
    IDistributedCache cache,
    IServiceScopeFactory scopeFactory,
    IChaosManager chaos, // [Phase 9] 심연의 맥박 연동
    ILogger<IdentityCacheService> logger) : IIdentityCacheService
{
    private const string StreamerKeyPrefix = "Streamer:";
    private const string ViewerKeyPrefix = "ViewerId:";
    private readonly DistributedCacheEntryOptions _streamerOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
    private readonly DistributedCacheEntryOptions _viewerOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };

    public async Task<StreamerProfile?> GetStreamerProfileAsync(string chzzkUid, CancellationToken ct = default)
    {
        // ⛈️ [혼돈의 시련]: 인위적 장애 주입 (테스트 모드 가동 시)
        await chaos.TryInjectFaultAsync("IdentityCache");

        string key = $"{StreamerKeyPrefix}{chzzkUid}";
        byte[]? raw = await cache.GetAsync(key, ct);

        if (raw != null)
        {
            return JsonSerializer.Deserialize<StreamerProfile>(raw);
        }

        // Cache Miss: DB 조회
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);
        if (profile != null)
        {
            await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(profile), _streamerOptions, ct);
            logger.LogDebug("🛡️ [이지스 캐시 로드] 스트리머 프로필: {ChzzkUid}", chzzkUid);
        }

        return profile;
    }

    public async Task<int> GetGlobalViewerIdAsync(string viewerUid, string nickname, CancellationToken ct = default)
    {
        var hash = Sha256Hasher.ComputeHash(viewerUid);
        string key = $"{ViewerKeyPrefix}{hash}";
        
        string? val = await cache.GetStringAsync(key, ct);
        if (int.TryParse(val, out int cachedId))
        {
            return cachedId;
        }

        // Cache Miss: DB 조회 (없을 경우 생성)
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var viewer = await db.GlobalViewers.FirstOrDefaultAsync(v => v.ViewerUidHash == hash, ct);
        if (viewer == null)
        {
            viewer = new GlobalViewer { ViewerUid = viewerUid, ViewerUidHash = hash, Nickname = nickname };
            db.GlobalViewers.Add(viewer);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("🆕 [이지스 신규 시청자 생성] {Nickname} (Hash: {Hash})", nickname, hash);
        }

        await cache.SetStringAsync(key, viewer.Id.ToString(), _viewerOptions, ct);
        return viewer.Id;
    }

    public async Task<string?> GetChzzkUidBySlugAsync(string slug, CancellationToken ct = default)
    {
        string key = $"Slug:{slug}";
        string? val = await cache.GetStringAsync(key, ct);
        
        if (val != null)
        {
            return val;
        }

        // Cache Miss: DB 조회 (지목한 슬러그를 가진 스트리머 검색)
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var uid = await db.StreamerProfiles
            .AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => p.ChzzkUid)
            .FirstOrDefaultAsync(ct);

        if (uid != null)
        {
            await cache.SetStringAsync(key, uid, _streamerOptions, ct);
            logger.LogDebug("🛡️ [이지스 역방향 색인 로드] Slug: {Slug} -> {Uid}", slug, uid);
        }

        return uid;
    }

    public void InvalidateStreamer(string chzzkUid)
    {
        cache.Remove($"{StreamerKeyPrefix}{chzzkUid}");
        logger.LogInformation("🛡️ [이지스 캐시 무효화] 스트리머 : {ChzzkUid}", chzzkUid);
    }

    public void InvalidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return;
        cache.Remove($"Slug:{slug}");
        logger.LogInformation("🛡️ [이지스 캐시 무효화] 슬러그 : {Slug}", slug);
    }
}
