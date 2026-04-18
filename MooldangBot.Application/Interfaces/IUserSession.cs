using System.Security.Claims;

namespace MooldangBot.Contracts.Common.Interfaces;

public interface IUserSession
{
    bool IsAuthenticated { get; }
    string? ChzzkUid { get; }
    string? Role { get; }
    List<string> AllowedChannelIds { get; }
}
