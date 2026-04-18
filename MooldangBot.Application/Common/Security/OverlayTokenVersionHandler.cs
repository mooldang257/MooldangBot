using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace MooldangBot.Application.Common.Security;

/// <summary>
/// [오시리스의 철퇴]: 토큰에 포함된 버전과 DB의 실시간 버전을 대조하여 무효화 여부를 판별합니다.
/// </summary>
public class OverlayTokenVersionRequirement : IAuthorizationRequirement { }

public class OverlayTokenVersionHandler(IServiceScopeFactory _scopeFactory, IDistributedCache _cache) : AuthorizationHandler<OverlayTokenVersionRequirement>
{
    private const string CacheKeyPrefix = "OverlayTokenVersion:";

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OverlayTokenVersionRequirement requirement)
    {
        var streamerIdClaim = context.User.FindFirst("StreamerId")?.Value;
        var tokenVersionClaim = context.User.FindFirst("TokenVersion")?.Value;

        if (string.IsNullOrEmpty(streamerIdClaim) || string.IsNullOrEmpty(tokenVersionClaim))
        {
            context.Fail();
            return;
        }

        // 1. Redis 캐시 확인 (오시리스의 기억)
        string cacheKey = $"{CacheKeyPrefix}{streamerIdClaim}";
        var cachedVersion = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedVersion))
        {
            if (cachedVersion == tokenVersionClaim)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return;
        }

        // 2. 캐시 미스 시 DB 조회 (전통적 방식)
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid == streamerIdClaim);

            if (streamer != null)
            {
                var currentVersion = streamer.OverlayTokenVersion.ToString();
                
                // 캐시 업데이트 (TTL: 10분)
                await _cache.SetStringAsync(cacheKey, currentVersion, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                if (currentVersion == tokenVersionClaim)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }
        }
    }
}
