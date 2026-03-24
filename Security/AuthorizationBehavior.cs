using MediatR;
using MooldangAPI.Data;

namespace MooldangAPI.Security;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUserSession _userSession;

    public AuthorizationBehavior(IUserSession userSession)
    {
        _userSession = userSession;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IAuthorizedRequest authorizedRequest)
        {
            if (!_userSession.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("인증이 필요합니다.");
            }

            // 마스터는 통과
            if (_userSession.Role == "master")
            {
                return await next();
            }

            var targetUid = authorizedRequest.ChzzkUid;
            if (string.IsNullOrEmpty(targetUid))
            {
                throw new UnauthorizedAccessException("대상 채널 식별자가 없습니다.");
            }

            if (!_userSession.AllowedChannelIds.Contains(targetUid, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"해당 채널({targetUid})에 대한 관리 권한이 없습니다.");
            }
        }

        return await next();
    }
}
