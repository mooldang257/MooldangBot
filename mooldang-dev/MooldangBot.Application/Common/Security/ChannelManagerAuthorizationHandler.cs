using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.Abstractions;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Common.Security;

/// <summary>
/// [오시리스의 선별]: 채널 관리 권한을 검증하는 핸들러입니다.
/// 마스터 권한 또는 해당 채널의 소유자/매니저인지 확인합니다. 슬러그 주소에 대한 자동 UID 변환을 지원합니다.
/// </summary>
public class ChannelManagerAuthorizationHandler(
    IUserSession _userSession, 
    IHttpContextAccessor _httpContextAccessor, 
    IAppDbContext _db,
    ILogger<ChannelManagerAuthorizationHandler> _logger) : AuthorizationHandler<ChannelManagerRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ChannelManagerRequirement requirement)
    {
        // 1. 마스터 권한 (Master / Bot)은 모든 채널 관리 가능 (프리패스)
        if (context.User.IsInRole("master"))
        {
            _logger.LogInformation("ChannelManagerPolicy: Master/Bot access granted for {User}", UserInfo(context));
            context.Succeed(requirement);
            return;
        }

        // 2. 요청 경로 또는 리소스에서 대상 대상 UID(chzzkUid) 추출
        var httpContext = _httpContextAccessor.HttpContext;
        var routeData = httpContext?.GetRouteData();
        
        // [물멍]: 다양한 경로 변수명 대응 (chzzkUid, uid, streamerId, streamerUid)
        var rawId = routeData?.Values["chzzkUid"]?.ToString() 
                    ?? routeData?.Values["uid"]?.ToString()
                    ?? routeData?.Values["streamerId"]?.ToString()
                    ?? routeData?.Values["streamerUid"]?.ToString();

        // 라우트 데이터에 없으면 리소스 확인
        if (string.IsNullOrEmpty(rawId) && context.Resource is string resourceUid)
        {
            rawId = resourceUid;
        }

        if (string.IsNullOrEmpty(rawId))
        {
            _logger.LogWarning("ChannelManagerPolicy: target UID not found. Path: {Path}", httpContext?.Request.Path);
            return;
        }

        var targetId = rawId.ToLower();
        var allowedIds = _userSession.AllowedChannelIds;

        // 3. 직접 일치 여부 확인 (UID가 직접 전달된 경우)
        if (allowedIds.Contains(targetId, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("ChannelManagerPolicy: Access GRANTED for {User} on Channel {Channel}", UserInfo(context), targetId);
            context.Succeed(requirement);
            return;
        }

        // 4. [슬러그 대응]: 전달된 ID가 슬러그일 경우 실제 UID로 변환하여 확인
        // [물멍]: 세션에 없는 ID가 들어왔을 때만 DB 조회를 수행하여 성능 부하를 최소화합니다.
        var resolvedUid = await _db.CoreStreamerProfiles
            .AsNoTracking()
            .Where(p => p.Slug != null && p.Slug.ToLower() == targetId)
            .Select(p => p.ChzzkUid)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(resolvedUid) && allowedIds.Contains(resolvedUid, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("ChannelManagerPolicy: Access GRANTED (via Slug: {Slug} -> UID: {Uid}) for {User}", targetId, resolvedUid, UserInfo(context));
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("ChannelManagerPolicy: Access DENIED for {User} on Target {Target}. IsAuth: {IsAuth}", 
                UserInfo(context), targetId, context.User.Identity?.IsAuthenticated);
        }
    }

    private string UserInfo(AuthorizationHandlerContext context) 
    {
        var identity = context.User.Identity;
        if (identity == null || !identity.IsAuthenticated) return "UnauthenticatedUser";
        
        var name = identity.Name ?? "NoName";
        var uid = _userSession.ChzzkUid ?? "NoUid";
        var role = _userSession.Role ?? "NoRole";
        
        return $"{name}({uid}/{role})";
    }
}
