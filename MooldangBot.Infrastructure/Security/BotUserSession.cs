using System.Collections.Generic;
using MooldangBot.Contracts.Common.Interfaces;

namespace MooldangBot.Infrastructure.Security;

/// <summary>
/// [v2.4.6] 봇 엔진(ChzzkAPI) 전용 시스템 사용자 세션
/// HTTP 컨텍스트가 없는 백그라운드 환경에서 AppDbContext의 의존성을 해결하기 위해 사용됩니다.
/// </summary>
public class BotUserSession : IUserSession
{
    public bool IsAuthenticated => false;

    // 시스템 작업을 나타내기 위해 null 또는 고정된 시스템 ID를 반환할 수 있습니다.
    public string? ChzzkUid => null;

    public string? Role => "System";

    public List<string> AllowedChannelIds => new List<string>();
}
