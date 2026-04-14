using System.Threading.Tasks;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Contracts.AI.Models;

namespace MooldangBot.Application.Common.Interfaces.Philosophy;

/// <summary>
/// [하모니의 조율]: 시스템 전반의 공명과 진동을 관리하는 인터페이스입니다.
/// </summary>
public interface IResonanceService
{
    /// <summary>
    /// 외부 자극(채팅 등)에 따른 시스템 진동수를 조정합니다.
    /// </summary>
    Task<bool> AdjustResonanceAsync(string chzzkUid, Vibration targetVibration);

    /// <summary>
    /// 현재 특정 스트리머 파로스의 상태를 수신합니다.
    /// </summary>
    Task<MooldangBot.Contracts.AI.Models.Parhos> GetCurrentParhosStateAsync(string chzzkUid);

    /// <summary>
    /// 현재 특정 스트리머의 안정도(Stability)에 따른 페르소나의 정의(Tone)를 수신합니다.
    /// </summary>
    string GetCurrentPersonaTone(string chzzkUid);

    /// <summary>
    /// 시스템 부하와 상호작용 횟수를 기반으로 동적 진동수를 산출합니다.
    /// </summary>
    double CalculateDynamicVibration(double systemLoad, int interactionCount);
}


