using Microsoft.AspNetCore.Authorization;
using MooldangAPI.Data;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace MooldangAPI.Security;

public class ChannelManagerAuthorizationHandler : AuthorizationHandler<ChannelManagerRequirement>
{
    private readonly IUserSession _userSession;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChannelManagerAuthorizationHandler> _logger;

    public ChannelManagerAuthorizationHandler(IUserSession userSession, IHttpContextAccessor httpContextAccessor, ILogger<ChannelManagerAuthorizationHandler> logger)
    {
        _userSession = userSession;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ChannelManagerRequirement requirement)
    {
        // 1. 마스터 권한 (Master / Bot)은 모든 채널 관리 가능 (프리패스)
        if (context.User.IsInRole("master"))
        {
            _logger.LogInformation("ChannelManagerPolicy: Master/Bot access granted for {User}", UserIdentityName(context));
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. 요청 경로 또는 리소스에서 대상 대상 UID(chzzkUid) 추출
        var httpContext = _httpContextAccessor.HttpContext;
        var routeData = httpContext?.GetRouteData();
        
        // 경로 변수 {chzzkUid} 또는 {uid} 확인
        var chzzkUid = routeData?.Values["chzzkUid"]?.ToString() 
                    ?? routeData?.Values["uid"]?.ToString();

        // 💡 [Gotcha 대응] 라우트 데이터에 없으면 리소스(컨트롤러에서 넘긴 값) 확인
        if (string.IsNullOrEmpty(chzzkUid) && context.Resource is string resourceUid)
        {
            chzzkUid = resourceUid;
        }

        if (string.IsNullOrEmpty(chzzkUid))
        {
            _logger.LogWarning("ChannelManagerPolicy: target UID not found. Path: {Path}", httpContext?.Request.Path);
            return Task.CompletedTask;
        }

        // 3. 현재 사용자가 해당 채널의 권한을 가지고 있는지 확인 (본인 포함)
        var allowedIds = _userSession.AllowedChannelIds;
        if (allowedIds.Contains(chzzkUid, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("ChannelManagerPolicy: Access granted for User {User} on Channel {Channel}", UserIdentityName(context), chzzkUid);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("ChannelManagerPolicy: Access denied for User {User} on Channel {Channel}", UserIdentityName(context), chzzkUid);
        }

        return Task.CompletedTask;
    }

    private string UserIdentityName(AuthorizationHandlerContext context) 
        => context.User.Identity?.Name ?? "Unknown";
}
