using System;
using System.Threading.Tasks;

namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [영겁의 열쇠]: 스트리머의 인증 토큰 생명주기를 관리하는 인터페이스입니다.
/// </summary>
public interface ITokenRenewalService
{
    /// <summary>
    /// [토큰의 확인]: 만료 시간이 임박했는지 확인하고 필요시 갱신을 수행합니다.
    /// </summary>
    /// <param name="chzzkUid">스트리머 UID</param>
    /// <returns>갱신 성공 여부</returns>
    Task<bool> RenewIfNeededAsync(string chzzkUid);

    /// <summary>
    /// [영겁의 열쇠]: 시간 체크를 무시하고 즉시 토큰 갱신을 수행합니다. (자가 치유 시 사용)
    /// </summary>
    Task<bool> RenewNowAsync(string chzzkUid);

    /// <summary>
    /// [회복력 점검]: 현재 인증 서버와의 서킷 브레이커 상태를 확인합니다.
    /// </summary>
    bool IsCircuitOpen();
}
