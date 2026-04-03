using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MooldangBot.Application.Common.Security;

/// <summary>
/// [오시리스의 철퇴]: 토큰에 포함된 버전과 DB의 실시간 버전을 대조하여 무효화 여부를 판별합니다.
/// </summary>
public class OverlayTokenVersionRequirement : IAuthorizationRequirement { }

public class OverlayTokenVersionHandler(IServiceScopeFactory _scopeFactory) : AuthorizationHandler<OverlayTokenVersionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OverlayTokenVersionRequirement requirement)
    {
        var streamerIdClaim = context.User.FindFirst("StreamerId")?.Value;
        var tokenVersionClaim = context.User.FindFirst("TokenVersion")?.Value;

        if (string.IsNullOrEmpty(streamerIdClaim) || string.IsNullOrEmpty(tokenVersionClaim))
        {
            context.Fail();
            return;
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            // DB에서 현재 유효한 버전을 조회 (캐싱 적용 고려 가능)
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid == streamerIdClaim);

            if (streamer != null && streamer.OverlayTokenVersion.ToString() == tokenVersionClaim)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}
