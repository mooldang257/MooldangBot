using MooldangBot.Foundation.Services;
using MooldangBot.Foundation.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Infrastructure.Workers.Points;
using MooldangBot.Infrastructure.Workers.Ledger;
using MooldangBot.Infrastructure.Workers.Broadcast;
using MooldangBot.Infrastructure.Workers.Maintenance;
using MooldangBot.Foundation.Workers;

namespace MooldangBot.Infrastructure.Workers;

public static class WorkerRegistry
{


    /// <summary>
    /// [v2.5.0] 백그라운드 비즈니스 워커 등록 (데이터 처리 및 유지보수 전담)
    /// </summary>
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 설정 등록
        var rootSection = configuration.GetSection("WorkerSettings");
        RegisterWorkerSettingsRecursive(services, rootSection);

        // 2. 비즈니스 로직 워커들
        services.AddHostedService<Points.PointWriteBackWorker>();
        
        // Chat Workers
        services.AddHostedService<Chat.ChatLogBatchWorker>();
        services.AddHostedService<Chat.LogBulkBufferWorker>();

        // // Core Workers (GateWay는 독립 위치 유지를 위해 여기서 배제함)
        // services.AddHostedService<ChzzkBackgroundService>();
        // services.AddHostedService<SystemWatchdogService>();

        // Broadcast Workers
        services.AddHostedService<Broadcast.TokenRenewalBackgroundService>();
        services.AddHostedService<Broadcast.PeriodicMessageWorker>();

        // Maintenance Workers
        services.AddHostedService<Maintenance.StagingCleanupWorker>();
        services.AddHostedService<Maintenance.RouletteLogCleanupService>();
        services.AddHostedService<Maintenance.ZeroingWorker>();
        services.AddHostedService<Maintenance.ChatLogCleanupWorker>();

        // Ledger & Analytics Workers
        services.AddHostedService<Ledger.CelestialLedgerWorker>();
        services.AddHostedService<Ledger.WeeklyStatsReporter>();

        // Module Workers 통합 (지휘관 지침: FuncRouletteMain 통합 - Infrastructure로 이관됨)
        services.AddHostedService<Maintenance.RouletteResultWorker>();

        return services;
    }



    /// <summary>
    /// [재귀적 등록]: 도메인별(Points, Chat 등)로 중첩된 설정을 찾아내어 워커 이름별로 옵션을 바인딩합니다.
    /// </summary>
    private static void RegisterWorkerSettingsRecursive(IServiceCollection services, IConfigurationSection section)
    {
        foreach (var child in section.GetChildren())
        {
            // IsEnabled가 존재하면 실제 워커 설정 섹션으로 간주하고 등록
            if (child.GetSection("IsEnabled").Exists())
            {
                services.Configure<WorkerSettings>(child.Key, child);
            }
            else
            {
                // 하위 섹션이 더 있다면 재귀 탐색
                RegisterWorkerSettingsRecursive(services, child);
            }
        }
    }
}