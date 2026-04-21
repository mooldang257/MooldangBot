using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using RedLockNet;
using MooldangBot.Infrastructure.Services;

namespace MooldangBot.Infrastructure.Workers.Core;

/// <summary>
/// [시스템 파수꾼]: 함대 인스턴스의 건강 상태를 감시하고 장애 시 긴급 알림을 전송합니다.
/// </summary>
public class SystemWatchdogService(
    IServiceProvider serviceProvider,
    HealthMonitorService healthMonitor,
    INotificationService notificationService,
    IDistributedLockFactory lockFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<SystemWatchdogService> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(SystemWatchdogService))
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    // [v15.1] 정기 보고 주기 관리 (6시간)
    private static DateTime _lastStatusReportAt = DateTime.MinValue;
    private static readonly TimeSpan _statusReportInterval = TimeSpan.FromHours(6);

    protected override int DefaultIntervalSeconds => 30;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        if (!await _semaphore.WaitAsync(0, ct))
        {
            _logger.LogWarning("[Watchdog] 이전 감시 작업이 지연되고 있습니다. 건너뜁니다.");
            return;
        }

        try
        {
            // 1. 세션/토큰 생존 신호 관리
            await MonitorAndRenewPulseAsync(ct);
            
            // 2. 함대 전역 건강검진 및 분산 알림 (Master Only)
            await CheckFleetHealthAndAlertAsync(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task MonitorAndRenewPulseAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChzzkChatClient>();

        var inactiveSessions = await db.BroadcastSessions
            .Include(s => s.StreamerProfile)
            .Where(s => s.IsActive && s.LastHeartbeatAt < KstClock.Now.AddMinutes(-5))
            .ToListAsync(ct);

        foreach (var session in inactiveSessions)
        {
            var chzzkUid = session.StreamerProfile?.ChzzkUid ?? "Unknown";
            _logger.LogWarning("[Watchdog] {ChzzkUid} 하트비트 단절. 세션 갈무리.", chzzkUid);
            await scribe.FinalizeSessionAsync(chzzkUid);
            await chatClient.DisconnectAsync(chzzkUid);
        }
    }

    private async Task CheckFleetHealthAndAlertAsync(CancellationToken ct)
    {
        var resource = "lock:watchdog:alert-master";
        var expiry = TimeSpan.FromSeconds(50);

        await using var redLock = await lockFactory.CreateLockAsync(resource, expiry);
        if (!redLock.IsAcquired) return;

        var report = await healthMonitor.GetSystemPulseAsync(ct);
        
        if (!report.Database || !report.Redis || !report.RabbitMQ)
        {
            string msg = $"🔥 [인프라 긴급] 함선 엔진 계통 고장!\nDB: {report.Database}, Redis: {report.Redis}, Rabbit: {report.RabbitMQ}";
            await notificationService.SendAlertAsync(msg, true, "infra:death", TimeSpan.FromMinutes(30));
        }

        foreach (var instance in report.FleetInstances.Values)
        {
            var machine = instance.MachineName;

            if ((DateTime.UtcNow - instance.LastSeenAt).TotalMinutes >= 5)
            {
                string msg = $"🚨 [함대 이탈] 인스턴스 [{machine}]의 신호가 끊겼습니다! (5분 경과)";
                await notificationService.SendAlertAsync(msg, true, $"lost:{machine}", TimeSpan.FromHours(1));
                continue;
            }

            if (instance.MemoryUsageMb > 1024)
            {
                string msg = $"🍔 [과식 경보] 인스턴스 [{machine}]가 과식 중입니다! (Memory: {instance.MemoryUsageMb}MB)";
                await notificationService.SendAlertAsync(msg, true, $"mem:{machine}", TimeSpan.FromHours(1));
            }

            foreach (var worker in instance.Workers)
            {
                if (!worker.Value)
                {
                    string msg = $"⚠️ [워커 정지] 인스턴스 [{machine}]의 '{worker.Key}' 워커가 정지되었습니다.";
                    await notificationService.SendAlertAsync(msg, true, $"worker:{machine}:{worker.Key}", TimeSpan.FromHours(1));
                }
            }
        }

        if (DateTime.UtcNow > _lastStatusReportAt.Add(_statusReportInterval))
        {
            _lastStatusReportAt = DateTime.UtcNow;
            string reportMsg = $"📊 [오시리스 정기 보고] 함대 전체의 맥박이 고르게 뛰고 있습니다.\n" +
                               $"가동 시간: {DateTime.UtcNow:O}\n" +
                               $"활성 인스턴스 수: {report.FleetInstances.Count}대\n" +
                               $"전체 맥박 상태: OK";
            
            await notificationService.SendAlertAsync(reportMsg, false, "status:periodic", TimeSpan.FromHours(6));
        }
    }
}
