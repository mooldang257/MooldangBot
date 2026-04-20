using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

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

    protected BaseHybridWorker(ILogger logger, IOptionsMonitor<WorkerSettings> optionsMonitor, string workerName)
    {
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

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _optionsMonitor.Get(_workerName);

            // 1. 활성화 여부 확인
            if (!settings.IsEnabled)
            {
                _logger.LogTrace("💤 [오시리스 엔진] {WorkerName} 일시 정지 상태 (IsEnabled: false)", _workerName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                // 2. 비즈니스 로직 수행
                await ProcessWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [오시리스 엔진] {WorkerName} 작업 중 충돌 발생", _workerName);
            }

            // 3. 지연 시간 산정 (지휘관 지침 vs 안전 기본값)
            // [지휘관 지시]: 최소 주기는 반드시 2초 이상이어야 합니다.
            int interval = settings.IntervalSeconds ?? DefaultIntervalSeconds;
            if (interval < 2)
            {
                if (settings.IntervalSeconds.HasValue)
                {
                   _logger.LogWarning("⚠️ [지휘 지침 보정] {WorkerName}의 주기({Interval}s)가 너무 짧아 안전 하한선(2s)으로 상향 조정합니다.", _workerName, interval);
                }
                interval = 2;
            }

            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }

        _logger.LogInformation("🛑 [오시리스 엔진] {WorkerName} 정지 보고", _workerName);
    }

    /// <summary>
    /// 실제 비즈니스 로직을 구현합니다.
    /// </summary>
    protected abstract Task ProcessWorkAsync(CancellationToken ct);
}
