namespace MooldangBot.Foundation.Workers;

/// <summary>
/// [파운데이션]: 워커들의 동작 제어를 위한 전역 설정 모델입니다.
/// </summary>
public class WorkerSettings
{
    public bool IsEnabled { get; init; } = true;
    public int? IntervalSeconds { get; init; } // null일 경우 워커 고유 기본값 사용
    public int MaxBatchSize { get; init; } = 1000;
}
