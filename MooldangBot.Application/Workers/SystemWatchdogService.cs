using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.Configuration;
using RedLockNet;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [이지스의 파수꾼]: 함대 전체의 상태를 감시하고 위급 상황 발생 시 선장님께 즉각 보고하는 조기 경보 서비스입니다.
/// [v15.0] RedLock 마스터 선출을 통해 중복 알림을 방지하며, 유연한 임계치 기반 경보를 수행합니다.
/// </summary>
public class SystemWatchdogService(
    IServiceProvider serviceProvider,
    IHealthMonitorService healthMonitor,
    INotificationService notificationService,
    IDistributedLockFactory lockFactory,
    ILogger<SystemWatchdogService> logger) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    // [v15.1] 정기 보고 주기 관리 (6시간)
    private static DateTime _lastStatusReportAt = DateTime.MinValue;
    private static readonly TimeSpan _statusReportInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🛡️ [이지스의 경보] 통합 와치독 가동되었습니다. (주기: 1분)");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!await _semaphore.WaitAsync(0, stoppingToken))
            {
                logger.LogWarning("[Watchdog] 이전 감시 작업이 지연되고 있습니다. 건너뜁니다.");
            }
            else
            {
                try
                {
                    // 1. [기존]: 세션/토큰 생존 신호 관리
                    await MonitorAndRenewPulseAsync(stoppingToken);
                    
                    // 2. [신규]: 함대 전역 건강검진 및 분산 알림 (Master Only)
                    await CheckFleetHealthAndAlertAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ [Watchdog] 감시 루프 오류 발생");
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task MonitorAndRenewPulseAsync(CancellationToken stoppingToken)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
            var chatClient = scope.ServiceProvider.GetRequiredService<IChzzkChatClient>();

            var inactiveSessions = db.BroadcastSessions
                .Include(s => s.StreamerProfile)
                .Where(s => s.IsActive && s.LastHeartbeatAt < KstClock.Now.AddMinutes(-5))
                .ToList();

            foreach (var session in inactiveSessions)
            {
                var chzzkUid = session.StreamerProfile?.ChzzkUid ?? "Unknown";
                logger.LogWarning("[Watchdog] {ChzzkUid} 하트비트 단절. 세션 갈무리.", chzzkUid);
                await scribe.FinalizeSessionAsync(chzzkUid);
                await chatClient.DisconnectAsync(chzzkUid);
            }
        }
    }

    private async Task CheckFleetHealthAndAlertAsync(CancellationToken ct)
    {
        // [v15.0] 분산 락을 통해 함대 내 단 한 대만 '알림 마스터' 역할 수행
        var resource = "lock:watchdog:alert-master";
        var expiry = TimeSpan.FromSeconds(50); // 주기(1분)보다 조금 짧게 설정

        await using var redLock = await lockFactory.CreateLockAsync(resource, expiry);
        if (!redLock.IsAcquired) return;

        var report = await healthMonitor.GetSystemPulseAsync(ct);
        
        // 1. 인프라 기반 장애 체크 (DB, Redis, RabbitMQ)
        if (!report.Database || !report.Redis || !report.RabbitMQ)
        {
            string msg = $"🔥 [인프라 긴급] 함선 엔진 계통 고장!\nDB: {report.Database}, Redis: {report.Redis}, Rabbit: {report.RabbitMQ}";
            await notificationService.SendAlertAsync(msg, true, "infra:death", TimeSpan.FromMinutes(30));
        }

        // 2. 개별 인스턴스 전수 조사
        foreach (var instance in report.FleetInstances.Values)
        {
            var machine = instance.MachineName;

            // [임계치: 함대 이탈] 5분 이상 무소식
            if ((DateTime.UtcNow - instance.LastSeenAt).TotalMinutes >= 5)
            {
                string msg = $"🚨 [함대 이탈] 인스턴스 [{machine}]의 신호가 끊겼습니다! (5분 경과)";
                await notificationService.SendAlertAsync(msg, true, $"lost:{machine}", TimeSpan.FromHours(1));
                continue;
            }

            // [임계치: 과식 경보] 메모리 1GB 초과
            if (instance.MemoryUsageMb > 1024)
            {
                string msg = $"🍔 [과식 경보] 인스턴스 [{machine}]가 과식 중입니다! (Memory: {instance.MemoryUsageMb}MB)";
                await notificationService.SendAlertAsync(msg, true, $"mem:{machine}", TimeSpan.FromHours(1));
            }

            // [임계치: 워커 정지]
            foreach (var worker in instance.Workers)
            {
                if (!worker.Value)
                {
                    string msg = $"⚠️ [워커 정지] 인스턴스 [{machine}]의 '{worker.Key}' 워커가 정지되었습니다.";
                    await notificationService.SendAlertAsync(msg, true, $"worker:{machine}:{worker.Key}", TimeSpan.FromHours(1));
                }
            }
        }

        // 3. 정기 상태 보고 (6시간 주기는 마스터가 전담)
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
