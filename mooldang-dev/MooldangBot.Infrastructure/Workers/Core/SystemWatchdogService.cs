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
    private bool _hasAnnouncedJoin = false;
    
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
            // 0. [신규] 함대 합류 보고 및 유령 맥박 청소 (기동 시 1회)
            if (!_hasAnnouncedJoin)
            {
                _hasAnnouncedJoin = true;
                string machine = (Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "Unknown") + ":" + Environment.MachineName;
                
                // [v23.0-Fix] 기동 전, 혹시 남아있을지 모르는 이전 실행의 유령 맥박을 청소합니다.
                await healthMonitor.CleanupInstanceAsync(machine, ct);
                
                await notificationService.SendAlertAsync($"🚀 [함대 합류] 인스턴스 [{machine}]가 가동을 시작했습니다.", NotificationChannel.Status, $"join:{machine}", TimeSpan.FromHours(1));
            }

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
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChzzkChatClient>();

        var inactiveSessions = await db.TableSysBroadcastSessions
            .Include(s => s.CoreStreamerProfiles)
            .Where(s => s.IsActive && s.LastHeartbeatAt < KstClock.Now.AddMinutes(-5))
            .ToListAsync(ct);

        foreach (var session in inactiveSessions)
        {
            var chzzkUid = session.CoreStreamerProfiles?.ChzzkUid ?? "Unknown";
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
            await notificationService.SendAlertAsync(msg, NotificationChannel.Critical, "infra:death", TimeSpan.FromMinutes(30));
        }

        foreach (var instance in report.FleetInstances.Values)
        {
            var machine = instance.MachineName;

            if ((DateTime.UtcNow - instance.LastSeenAt).TotalMinutes >= 5)
            {
                string msg = $"🚨 [함대 이탈] 인스턴스 [{machine}]의 신호가 끊겼습니다! (5분 경과)";
                await notificationService.SendAlertAsync(msg, NotificationChannel.Critical, $"lost:{machine}", TimeSpan.FromHours(1));

                // [v15.2] 10분 이상 부재 시 유령 데이터 자동 청소
                if ((DateTime.UtcNow - instance.LastSeenAt).TotalMinutes >= 10)
                {
                    _logger.LogInformation("[Watchdog] 인스턴스 [{Machine}] 장기 부재. Redis 유령 데이터 청소.", machine);
                    await healthMonitor.CleanupInstanceAsync(machine, ct);
                }
                continue;
            }

            if (instance.MemoryUsageMb > 1024)
            {
                string msg = $"🍔 [과식 경보] 인스턴스 [{machine}]가 과식 중입니다! (Memory: {instance.MemoryUsageMb}MB)";
                await notificationService.SendAlertAsync(msg, NotificationChannel.Critical, $"mem:{machine}", TimeSpan.FromHours(1));
            }

            // [v15.3] 알림 소음 억제: 모든 워커가 정지된 경우 인스턴스 전체의 침묵으로 간주하여 요약 보고합니다.
            var workers = instance.Workers;
            var stoppedWorkers = workers.Where(w => !w.Value).Select(w => w.Key).ToList();

            if (stoppedWorkers.Count > 0)
            {
                if (stoppedWorkers.Count == workers.Count)
                {
                    // 모든 워커가 침묵함 -> 인스턴스 전체 장애/정지 가능성 농후
                    string msg = $"⚠️ [인스턴스 침묵] 인스턴스 [{machine}]의 모든 워커({workers.Count}개)가 응답하지 않습니다. 전체 정지 또는 네트워크 단절이 의심됩니다.";
                    await notificationService.SendAlertAsync(msg, NotificationChannel.Critical, $"instance_halt:{machine}", TimeSpan.FromHours(1));
                }
                else if (stoppedWorkers.Count >= 3)
                {
                    // [v25.0-Fix] 3개 이상 워커 정지 시 요약 보고 (알림 소음 억제)
                    string msg = $"⚠️ [인스턴스 부분 장애] 인스턴스 [{machine}]의 워커 {stoppedWorkers.Count}개가 응답하지 않습니다.\n(대상: {string.Join(", ", stoppedWorkers)})";
                    await notificationService.SendAlertAsync(msg, NotificationChannel.Status, $"summary-stop:{machine}", TimeSpan.FromMinutes(30));
                }
                else
                {
                    // 일부 워커(2개 이하)만 문제 -> 개별 상세 보고
                    foreach (var workerName in stoppedWorkers)
                    {
                        string msg = $"⚠️ [워커 정지] 인스턴스 [{machine}]의 '{workerName}' 워커가 정지되었습니다.";
                        await notificationService.SendAlertAsync(msg, NotificationChannel.Critical, $"worker:{machine}:{workerName}", TimeSpan.FromHours(1));
                    }
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
            
            await notificationService.SendAlertAsync(reportMsg, NotificationChannel.Status, "status:periodic", TimeSpan.FromHours(6));
        }
    }
}
