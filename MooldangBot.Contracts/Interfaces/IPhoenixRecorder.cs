namespace MooldangBot.Contracts.Interfaces;

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
