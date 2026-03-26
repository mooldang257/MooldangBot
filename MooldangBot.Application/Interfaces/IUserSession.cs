using System.Security.Claims;

namespace MooldangBot.Application.Interfaces
{
    public interface IUserSession
    {
        string? ChzzkUid { get; }
        bool IsAuthenticated { get; }
        string? Role { get; } // "streamer", "manager", "admin", "superadmin", "master"
        IEnumerable<string> AllowedChannelIds { get; } // 관리 권한이 있는 채널 Uid 목록
    }
}
