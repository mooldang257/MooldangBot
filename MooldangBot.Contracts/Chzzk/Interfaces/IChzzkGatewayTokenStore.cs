using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Contracts.Chzzk.Interfaces;

/// <summary>
/// [오시리스의 영혼 - 게이트웨이]: 치지직 서버와의 저수준 통신을 위한 토큰 및 쿠키를 관리하는 인터페이스입니다.
/// </summary>
public interface IChzzkGatewayTokenStore
{
    /// <summary>
    /// 특정 채널의 토큰/쿠키를 저장합니다.
    /// </summary>
    Task SetTokenAsync(string chzzkUid, string sessionCookie, string authCookie);

    /// <summary>
    /// 특정 채널의 토큰/쿠키를 조회합니다.
    /// </summary>
    Task<(string SessionCookie, string AuthCookie)> GetTokenAsync(string chzzkUid);

    /// <summary>
    /// 현재 관리 중인 모든 채널의 토큰 정보를 가져옵니다.
    /// </summary>
    Task<IDictionary<string, (string SessionCookie, string AuthCookie)>> GetAllTokensAsync();
}
