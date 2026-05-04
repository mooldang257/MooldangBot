using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using MooldangBot.Foundation.Services;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Foundation.Workers;

/// <summary>
/// [파운데이션]: 모든 범용 워커의 기초 엔진입니다.
/// </summary>
public abstract class BaseHybridWorker : BackgroundService
{
    protected readonly ILogger _logger;
    protected readonly IOptionsMonitor<WorkerSettings> _optionsMonitor;
    protected readonly string _workerName;
    protected readonly IServiceProvider _serviceProvider;

    protected virtual bool RequiresDistributedLock => false;
    protected virtual string LockResourceName => $"lock:worker:{_workerName}";
    protected virtual TimeSpan LockExpiry => TimeSpan.FromSeconds(DefaultIntervalSeconds - 1);

    protected BaseHybridWorker(IServiceProvider serviceProvider, ILogger logger, IOptionsMonitor<WorkerSettings> optionsMonitor, string workerName)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _workerName = workerName;
    }

    protected abstract int DefaultIntervalSeconds { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [Foundation] {WorkerName} 기동 시작", _workerName);

        // 하트비트 루프
        _ = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pulse = _serviceProvider.GetService<PulseService>();
                    pulse?.ReportPulse(_workerName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "💓 [Pulse] {WorkerName} 보고 중 오류", _workerName);
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = _optionsMonitor.Get(_workerName);
                if (!settings.IsEnabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                if (RequiresDistributedLock)
                {
                    var lockFactory = _serviceProvider.GetService<IDistributedLockFactory>();
                    if (lockFactory != null)
                    {
                        await using var redLock = await lockFactory.CreateLockAsync(LockResourceName, LockExpiry);
                        if (!redLock.IsAcquired)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                            continue;
                        }
                        await ProcessWorkAsync(stoppingToken);
                    }
                    else
                    {
                        await ProcessWorkAsync(stoppingToken);
                    }
                }
                else
                {
                    await ProcessWorkAsync(stoppingToken);
                }

                int interval = settings.IntervalSeconds ?? DefaultIntervalSeconds;
                if (interval < 2) interval = 2;
                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [Foundation] {WorkerName} 실행 중 오류 발생", _workerName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    protected abstract Task ProcessWorkAsync(CancellationToken ct);
}
