namespace MooldangBot.Infrastructure.Workers;

public class WorkerSettings
{
    public bool IsEnabled { get; init; } = true;
    public int? IntervalSeconds { get; init; } // null일 경우 워커 고유 기본값 사용
    public int MaxBatchSize { get; init; } = 1000; // 기본값
}