using System.Security.Claims;

namespace MooldangBot.Domain.Abstractions;

public interface IUserSession
{
    bool IsAuthenticated { get; }
    string? ChzzkUid { get; }
    string? Role { get; }
    List<string> AllowedChannelIds { get; }
}
