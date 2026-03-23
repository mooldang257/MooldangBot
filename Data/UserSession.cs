using System.Security.Claims;

namespace MooldangAPI.Data
{
    public class UserSession : IUserSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? ChzzkUid 
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                // AuthController에서 "StreamerId" 클레임으로 저장하고 있음
                return user?.FindFirstValue("StreamerId");
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
