using Microsoft.AspNetCore.Authorization;
using MooldangAPI.Data;
using System.Security.Claims;

namespace MooldangAPI.Security;

public class ChannelManagerAuthorizationHandler : AuthorizationHandler<ChannelManagerRequirement>
{
    private readonly IUserSession _userSession;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChannelManagerAuthorizationHandler(IUserSession userSession, IHttpContextAccessor httpContextAccessor)
    {
        _userSession = userSession;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ChannelManagerRequirement requirement)
    {
        // 1. 마스터 권한은 모든 채널 관리 가능
        if (_userSession.Role == "master")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. 요청 경로에서 chzzkUid 추출 (예: /api/chatpoint/{chzzkUid})
        var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
        var chzzkUid = routeData?.Values["chzzkUid"]?.ToString();

        if (string.IsNullOrEmpty(chzzkUid))
        {
            // 경로에 Uid가 없으면 일단 통과 (다른 검증에 맡김) 또는 실패
            // 여기서는 채널 관리 정책이므로 Uid가 필수라고 가정
            return Task.CompletedTask;
        }

        // 3. 현재 사용자가 해당 채널의 관리 권한(AllowedChannelId)을 가지고 있는지 확인
        if (_userSession.AllowedChannelIds.Contains(chzzkUid, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
