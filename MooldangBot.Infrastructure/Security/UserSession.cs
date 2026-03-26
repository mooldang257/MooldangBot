using Microsoft.AspNetCore.Http;
using MooldangBot.Application.Interfaces;
using System.Security.Claims;

namespace MooldangBot.Infrastructure.Security;

public class UserSession : IUserSession
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSession(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    public string? ChzzkUid => _httpContextAccessor.HttpContext?.User.FindFirst("StreamerId")?.Value;
    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public List<string> AllowedChannelIds => 
        _httpContextAccessor.HttpContext?.User.FindAll("AllowedChannel")
        .Select(c => c.Value)
        .ToList() ?? new List<string>();
}