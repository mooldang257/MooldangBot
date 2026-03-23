using System.Security.Claims;

namespace MooldangAPI.Data
{
    public interface IUserSession
    {
        string? ChzzkUid { get; }
        bool IsAuthenticated { get; }
    }
}
