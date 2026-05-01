using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using MooldangBot.Application.Services;

namespace MooldangBot.Infrastructure.Workers;

/// <summary>
/// [오시리스의 표준 엔진]: 모든 범용 워커의 부모 클래스입니다.
/// 하이브리드 제어(설정 적용 + 안전 기본값) 및 강제 안전 하한선(2초)을 제공합니다.
/// </summary>
public abstract class BaseHybridWorker : BackgroundService
{
    protected readonly ILogger _logger;
    protected readonly IOptionsMonitor<WorkerSettings> _optionsMonitor;
    protected readonly string _workerName;
    protected readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 분산 잠금(RedLock) 활성화 여부입니다. (자식 워커가 오버라이드합니다)
    /// </summary>
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

    /// <summary>
    /// 워커 고유의 안전 기본 주기를 정의합니다. (설정이 없을 경우 사용)
    /// </summary>
    protected abstract int DefaultIntervalSeconds { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [오시리스 엔진] {WorkerName} 기동 시작 (지휘관 지침 대기 중)", _workerName);

        // [v28.0-Fix] 하트비트(맥박) 전용 루프를 메인 작업과 분리하여 실행합니다.
        // 이를 통해 24시간 주기 같은 장기 대기 워커들도 '생존 신호'를 꾸준히 보낼 수 있습니다.
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
                    _logger.LogWarning(ex, "💓 [하트비트] {WorkerName} 맥박 보고 중 일시적 오류", _workerName);
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = _optionsMonitor.Get(_workerName);

                // 1. 활성화 여부 확인
                if (!settings.IsEnabled)
                {
                    _logger.LogTrace("💤 [오시리스 엔진] {WorkerName} 일시 정지 상태 (IsEnabled: false)", _workerName);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                // 2. 비즈니스 로직 수행 (분산 잠금 확인)
                if (RequiresDistributedLock)
                {
                    var lockFactory = _serviceProvider.GetService<IDistributedLockFactory>();
                    if (lockFactory != null)
                    {
                        var adjustedExpiry = LockExpiry.TotalSeconds <= 0 ? TimeSpan.FromSeconds(1) : LockExpiry;
                        await using var redLock = await lockFactory.CreateLockAsync(LockResourceName, adjustedExpiry);
                        if (!redLock.IsAcquired)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                            continue;
                        }
                        await ProcessWorkAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [{WorkerName}] 분산 잠금이 요구되었으나 IDistributedLockFactory가 주입되지 않았습니다. 일반 모드로 실행합니다.", _workerName);
                        await ProcessWorkAsync(stoppingToken);
                    }
                }
                else
                {
                    await ProcessWorkAsync(stoppingToken);
                }

                // 3. 지연 시간 산정 (지휘관 지침 vs 안전 기본값)
                int interval = settings.IntervalSeconds ?? DefaultIntervalSeconds;
                if (interval < 2) interval = 2;

                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [오시리스 엔진] {WorkerName} 실행 중 치명적 오류 발생 (자동 복구 시도)", _workerName);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(DefaultIntervalSeconds, 60)), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 [오시리스 엔진] {WorkerName} 정지 보고", _workerName);
    }

    /// <summary>
    /// 실제 비즈니스 로직을 구현합니다.
    /// </summary>
    protected abstract Task ProcessWorkAsync(CancellationToken ct);
}
