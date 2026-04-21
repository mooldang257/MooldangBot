using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Security
{
    /// <summary>
    /// [이지스 쉴드]: 스트리머 채널 접근 권한을 중앙에서 검증하는 핵심 핸들러입니다.
    /// URL 경로의 {chzzkUid}를 추출하여 현재 사용자의 권한과 대조합니다.
    /// </summary>
    public class StreamerAccessHandler(
        IUserSession _userSession, 
        IHttpContextAccessor _httpContextAccessor, 
        IAppDbContext _db,
        IIdentityCacheService _identityCache,
        ILogger<StreamerAccessHandler> _logger) : AuthorizationHandler<StreamerAccessRequirement>
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, StreamerAccessRequirement requirement)
        {
            // 1. 마스터 권한 (Master / Bot)은 모든 채널 관리 가능 (전지적 권한)
            if (context.User.IsInRole("master"))
            {
                _logger.LogInformation("[이지스 쉴드] Master access granted for {User}", UserInfo(context));
                context.Succeed(requirement);
                return;
            }

            // 2. 요청 경로에서 대상 UID(chzzkUid) 추출
            var httpContext = _httpContextAccessor.HttpContext;
            var routeData = httpContext?.GetRouteData();
            
            // [물멍]: 경로 변수명 대응 (chzzkUid를 표준으로 사용)
            var rawId = routeData?.Values["chzzkUid"]?.ToString() 
                        ?? routeData?.Values["uid"]?.ToString()
                        ?? httpContext?.Request.Query["chzzkUid"].ToString();

            if (string.IsNullOrEmpty(rawId))
            {
                _logger.LogWarning("[이지스 쉴드] Target UID not found in route or query. Path: {Path}", httpContext?.Request.Path);
                return;
            }

            var targetId = rawId.ToLower();
            var allowedIds = _userSession.AllowedChannelIds;

            // 3. 직접 일치 여부 확인
            if (allowedIds.Contains(targetId, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[이지스 쉴드] Access GRANTED for {User} on Channel {Channel}", UserInfo(context), targetId);
                context.Succeed(requirement);
                return;
            }

            // 4. [슬러그 주소 대응]: 전달된 ID가 슬러그일 경우 실제 UID로 변환하여 확인
            // [이지스 캐시]: DB를 직접 뒤지지 않고 인메모리 캐시 서비스를 활용합니다.
            var resolvedUid = await _identityCache.GetChzzkUidBySlugAsync(targetId);

            if (!string.IsNullOrEmpty(resolvedUid) && allowedIds.Contains(resolvedUid, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[이지스 쉴드] Access GRANTED (Slug: {Slug} -> UID: {Uid}) for {User}", targetId, resolvedUid, UserInfo(context));
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("[이지스 쉴드] Access DENIED for {User} on Target {Target}", 
                    UserInfo(context), targetId);
            }
        }

        private string UserInfo(AuthorizationHandlerContext context) 
        {
            var identity = context.User.Identity;
            if (identity == null || !identity.IsAuthenticated) return "Unauthenticated";
            
            var uid = _userSession.ChzzkUid ?? "NoUid";
            return $"{identity.Name}({uid})";
        }
    }
}
