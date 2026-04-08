using MooldangBot.Application.Interfaces;

namespace MooldangBot.Infrastructure.Security;

/// <summary>
/// [v2.4.6] 봇 엔진 및 백그라운드 워커 전용 시스템 세션
/// HTTP 컨텍스트가 없는 환경에서 AppDbContext가 요구하는 IUserSession을 충족시킵니다.
/// </summary>
public class BotUserSession : IUserSession
{
    public bool IsAuthenticated => false;
    public string? ChzzkUid => null;
    public string? Role => "System";
    public List<string> AllowedChannelIds => new List<string>();
}
