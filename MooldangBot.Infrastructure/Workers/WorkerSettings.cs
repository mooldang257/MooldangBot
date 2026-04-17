namespace MooldangBot.Infrastructure.Workers;

public class WorkerSettings
{
    public bool IsEnabled { get; init; } = true;
    public int IntervalSeconds { get; init; } = 5; // 기본값
    public int MaxBatchSize { get; init; } = 1000; // 기본값
}