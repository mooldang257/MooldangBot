using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using RedLockNet;
using MooldangBot.Foundation.Services;
using MooldangBot.Foundation.Persistence;

namespace MooldangBot.Foundation.Workers;

/// <summary>
/// [파운데이션]: 시스템 전반의 건강 상태를 감시하고 장애 시 알림을 전송합니다.
/// </summary>
public class SystemWatchdogService(
    IServiceProvider serviceProvider,
    INotificationService notificationService,
    IDistributedLockFactory lockFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<SystemWatchdogService> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(SystemWatchdogService))
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _hasAnnouncedJoin = false;
    
    protected override int DefaultIntervalSeconds => 30;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        if (!await _semaphore.WaitAsync(0, ct)) return;

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var healthMonitor = scope.ServiceProvider.GetRequiredService<HealthMonitorService>();
                
                if (!_hasAnnouncedJoin)
                {
                    _hasAnnouncedJoin = true;
                    string machine = (Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "Unknown") + ":" + Environment.MachineName;
                    await healthMonitor.CleanupInstanceAsync(machine, ct);
                    await notificationService.SendAlertAsync($"🚀 [함대 합류] 인스턴스 [{machine}] 가동 시작", NotificationChannel.Status, $"join:{machine}", TimeSpan.FromHours(1));
                }

                await CheckFleetHealthAndAlertAsync(healthMonitor, ct);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CheckFleetHealthAndAlertAsync(HealthMonitorService healthMonitor, CancellationToken ct)
    {
        var report = await healthMonitor.GetSystemPulseAsync(ct);
        
        if (!report.Database || !report.Redis || !report.RabbitMQ)
        {
            string msg = $"🔥 [인프라 긴급] 인프라 고장 감지!\nDB: {report.Database}, Redis: {report.Redis}, Rabbit: {report.RabbitMQ}";
            await notificationService.SendAlertAsync(msg, NotificationChannel.Critical, "infra:death", TimeSpan.FromMinutes(30));
        }

        // 추가적인 함대 상태 체크 로직...
    }
}
