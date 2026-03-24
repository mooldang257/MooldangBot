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
        // 1. 마스터 권한은 모든 채널 관리 가능
        if (_userSession.Role == "master")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. 요청 경로에서 chzzkUid 추출
        var httpContext = _httpContextAccessor.HttpContext;
        var routeData = httpContext?.GetRouteData();
        
        // "chzzkUid" 뿐만 아니라 대소문자 변형이나 "uid" 등도 확인할 수 있도록 유연하게 처리 (현재는 chzzkUid 고정)
        var chzzkUid = routeData?.Values["chzzkUid"]?.ToString();

        if (string.IsNullOrEmpty(chzzkUid))
        {
            _logger.LogWarning("ChannelManagerPolicy: chzzkUid not found in route data. Path: {Path}", httpContext?.Request.Path);
            return Task.CompletedTask;
        }

        // 3. 현재 사용자가 해당 채널의 관리 권한(AllowedChannelId)을 가지고 있는지 확인
        var allowedIds = _userSession.AllowedChannelIds;
        if (allowedIds.Contains(chzzkUid, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("ChannelManagerPolicy: Success for User {User} on Channel {Channel}", UserIdentityName(context), chzzkUid);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("ChannelManagerPolicy: Denied for User {User} on Channel {Channel}. Allowed: {Allowed}", 
                UserIdentityName(context), chzzkUid, string.Join(", ", allowedIds));
        }

        return Task.CompletedTask;
    }

    private string UserIdentityName(AuthorizationHandlerContext context) 
        => context.User.Identity?.Name ?? "Unknown";
}
