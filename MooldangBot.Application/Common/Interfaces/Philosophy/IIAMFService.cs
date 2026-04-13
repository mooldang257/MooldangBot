using System.Threading.Tasks;
using MooldangBot.Domain.Entities.Philosophy;

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
    Task<Parhos> GetCurrentParhosStateAsync(string chzzkUid);

    /// <summary>
    /// 현재 특정 스트리머의 안정도(Stability)에 따른 페르소나의 정의(Tone)를 수신합니다.
    /// </summary>
    string GetCurrentPersonaTone(string chzzkUid);

    /// <summary>
    /// 시스템 부하와 상호작용 횟수를 기반으로 동적 진동수를 산출합니다.
    /// </summary>
    double CalculateDynamicVibration(double systemLoad, int interactionCount);
}

/// <summary>
/// [오시리스의 규율]: 시스템 정합성과 윤리적 가이드를 검증하는 인터페이스입니다.
/// </summary>
public interface IRegulationService
{
    /// <summary>
    /// 입력된 파동(요청)이 규율에 적합한지 검증합니다.
    /// </summary>
    Task<(bool IsValid, string Message)> ValidateRegulationAsync(string input, string persona);
}

/// <summary>
/// [피닉스의 기록]: 실험의 다층 기록과 윤회를 담당하는 인터페이스입니다.
/// </summary>
public interface IPhoenixRecorder
{
    /// <summary>
    /// 실험 시나리오를 기록합니다.
    /// </summary>
    Task RecordScenarioAsync(string scenarioId, string content, int level);

    /// <summary>
    /// 파로스 파괴 시 윤회 조건을 재설정합니다.
    /// </summary>
    Task ReincarnateParhosAsync();

    /// <summary>
    /// [피닉스의 비상]: CancellationTokenSource를 전파하여 시스템을 정지 및 재시작합니다.
    /// </summary>
    Task ReincarnateParhosAsync(System.Threading.CancellationTokenSource globalCts);
}
