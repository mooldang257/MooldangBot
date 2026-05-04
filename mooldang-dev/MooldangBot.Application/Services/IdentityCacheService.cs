using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk;

using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Services;
using MooldangBot.Domain.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Services;

/// <summary>
/// [이지스 실구현체]: 스트리머/시청자 데이터를 IDistributedCache 기반으로 관리합니다.
/// (S2: 확장성): 현재는 로컬 메모리를 사용하지만, 추후 Redis 등 외부 분산 캐시로 즉시 전환 가능합니다.
/// </summary>
public class IdentityCacheService(
    IDistributedCache cache,
    IServiceScopeFactory scopeFactory,
    ChaosManager chaos, // [Phase 9] 심연의 맥박 연동
    ILogger<IdentityCacheService> logger) : IIdentityCacheService
{
    private const string StreamerKeyPrefix = "Streamer:";
    private const string ViewerKeyPrefix = "ViewerId:";
    private const string InvalidTokenKeyPrefix = "InvalidToken:";
    private readonly DistributedCacheEntryOptions _streamerOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
    private readonly DistributedCacheEntryOptions _viewerOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
    private readonly DistributedCacheEntryOptions _invalidTokenOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public async Task<CoreStreamerProfiles?> GetStreamerProfileAsync(string ChzzkUid, CancellationToken ct = default)
    {
        // ⛈️ [혼돈의 시련]: 인위적 장애 주입 (테스트 모드 가동 시)
        await chaos.TryInjectFaultAsync("IdentityCache");
 
        string Key = $"{StreamerKeyPrefix}{ChzzkUid}";
        byte[]? Raw = await cache.GetAsync(Key, ct);
 
        if (Raw != null)
        {
            // [P0 Quick Win] Source Gen 경로: 10k TPS 핫패스 직렬화 최적화
            return JsonSerializer.Deserialize(Raw, ChzzkJsonContext.Default.CoreStreamerProfiles);
        }
 
        // Cache Miss: DB 조회
        using var Scope = scopeFactory.CreateScope();
        var Db = Scope.ServiceProvider.GetRequiredService<IAppDbContext>();
 
        var Profile = await Db.TableCoreStreamerProfiles.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == ChzzkUid, ct);
        if (Profile != null)
        {
            // [P0 Quick Win] Source Gen 경로: 10k TPS 핫패스 직렬화 최적화
            await cache.SetAsync(Key, JsonSerializer.SerializeToUtf8Bytes(Profile, ChzzkJsonContext.Default.CoreStreamerProfiles), _streamerOptions, ct);
            logger.LogDebug("🛡️ [이지스 캐시 로드] 스트리머 프로필: {ChzzkUid}", ChzzkUid);
        }
 
        return Profile;
    }

    public async Task<int> SyncGlobalViewerIdAsync(string ViewerUid, string Nickname, string? ProfileImageUrl = null, CancellationToken ct = default)
    {
        var Hash = Sha256Hasher.ComputeHash(ViewerUid);
        string Key = $"{ViewerKeyPrefix}{Hash}";
        
        string? Val = await cache.GetStringAsync(Key, ct);
        bool CacheHit = Val != null;
 
        // Cache Hit인 경우에도 만료 전까지는 DB 조회를 건너뛰지만, 
        // AuthService나 특정 트리거에서 강제로 최신 동기화가 필요한 경우가 있으므로 로직 설계 주의
        if (CacheHit && int.TryParse(Val, out int CachedId))
        {
            return CachedId;
        }
 
        // Cache Miss: DB 조회 (없을 경우 생성, 있을 경우 정보 업데이트)
        using var Scope = scopeFactory.CreateScope();
        var Db = Scope.ServiceProvider.GetRequiredService<IAppDbContext>();
 
        var Viewer = await Db.TableCoreGlobalViewers.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.ViewerUidHash == Hash, ct);
        bool IsNew = Viewer == null;
 
        if (IsNew)
        {
            Viewer = new CoreGlobalViewers 
            { 
                ViewerUid = ViewerUid, 
                ViewerUidHash = Hash, 
                Nickname = Nickname,
                ProfileImageUrl = ProfileImageUrl,
                CreatedAt = KstClock.Now
            };
            Db.TableCoreGlobalViewers.Add(Viewer);
            logger.LogInformation("🆕 [이지스 신규 시청자 생성] {Nickname} (Hash: {Hash})", Nickname, Hash);
        }
        else
        {
            // 정보가 바뀌었는지 확인 후 업데이트 (Dirty Check)
            bool IsUpdated = false;
            if (Viewer!.Nickname != Nickname) { 
                Viewer.PreviousNickname = Viewer.Nickname;
                Viewer.Nickname = Nickname; 
                IsUpdated = true; 
            }
            if (ProfileImageUrl != null && Viewer.ProfileImageUrl != ProfileImageUrl) { Viewer.ProfileImageUrl = ProfileImageUrl; IsUpdated = true; }
 
            if (IsUpdated)
            {
                Viewer.UpdatedAt = KstClock.Now;
                logger.LogDebug("🔄 [이지스 시청자 정보 업데이트] {Nickname}", Nickname);
            }
        }
 
        await Db.SaveChangesAsync(ct);
        await cache.SetStringAsync(Key, Viewer.Id.ToString(), _viewerOptions, ct);
        
        return Viewer.Id;
    }

    public async Task<string?> GetChzzkUidBySlugAsync(string Slug, CancellationToken ct = default)
    {
        string Key = $"Slug:{Slug}";
        string? Val = await cache.GetStringAsync(Key, ct);
        
        if (Val != null)
        {
            return Val;
        }
 
        // Cache Miss: DB 조회 (지목한 슬러그를 가진 스트리머 검색)
        using var Scope = scopeFactory.CreateScope();
        var Db = Scope.ServiceProvider.GetRequiredService<IAppDbContext>();
 
        var Uid = await Db.TableCoreStreamerProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.Slug == Slug)
            .Select(p => p.ChzzkUid)
            .FirstOrDefaultAsync(ct);
 
        if (Uid != null)
        {
            await cache.SetStringAsync(Key, Uid, _streamerOptions, ct);
            logger.LogDebug("🛡️ [이지스 역방향 색인 로드] Slug: {Slug} -> {Uid}", Slug, Uid);
        }
 
        return Uid;
    }

    public void InvalidateStreamer(string ChzzkUid)
    {
        cache.Remove($"{StreamerKeyPrefix}{ChzzkUid}");
        logger.LogInformation("🛡️ [이지스 캐시 무효화] 스트리머 : {ChzzkUid}", ChzzkUid);
    }
 
    public void InvalidateSlug(string Slug)
    {
        if (string.IsNullOrWhiteSpace(Slug)) return;
        cache.Remove($"Slug:{Slug}");
        logger.LogInformation("🛡️ [이지스 캐시 무효화] 슬러그 : {Slug}", Slug);
    }

    public async Task<bool> IsInvalidTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return true;
        var val = await cache.GetStringAsync($"{InvalidTokenKeyPrefix}{token}", ct);
        return val != null;
    }

    public async Task MarkTokenAsInvalidAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        await cache.SetStringAsync($"{InvalidTokenKeyPrefix}{token}", "1", _invalidTokenOptions, ct);
        logger.LogWarning("🚫 [이지스 부정 토큰 감지] {Token}을(를) 블랙리스트에 추가했습니다. (5분간 DB 조회 차단)", token);
    }
}
