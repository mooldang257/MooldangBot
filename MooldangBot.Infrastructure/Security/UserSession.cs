using Microsoft.AspNetCore.Http;
using MooldangBot.Application.Interfaces;
using System.Security.Claims;
using System.Linq;

namespace MooldangBot.Infrastructure.Security
{
    public class UserSession : IUserSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string? ChzzkUid => User?.FindFirst("StreamerId")?.Value;

        public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

        public IEnumerable<string> AllowedChannelIds => 
            User?.FindAll("AllowedChannelId").Select(c => c.Value) ?? Enumerable.Empty<string>();

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}