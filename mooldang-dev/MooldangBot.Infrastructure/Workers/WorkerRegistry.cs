using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Infrastructure.Workers.Points;
using MooldangBot.Infrastructure.Workers.Core;
using MooldangBot.Infrastructure.Workers.Ledger;
using MooldangBot.Infrastructure.Workers.Broadcast;
using MooldangBot.Infrastructure.Workers.Maintenance;

namespace MooldangBot.Infrastructure.Workers;

public static class WorkerRegistry
{
    /// <summary>
    /// [v2.5.0] 코어 봇 워커 등록 (치지직 통신 전담)
    /// </summary>
    public static IServiceCollection AddCoreBotWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<ChzzkBackgroundService>();
        services.AddHostedService<SystemWatchdogService>();
        return services;
    }

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
        services.AddHostedService<Chat.ChatLogBatchWorker>();
        services.AddHostedService<Chat.LogBulkBufferWorker>();
        services.AddHostedService<Broadcast.TokenRenewalBackgroundService>();
        services.AddHostedService<Broadcast.PeriodicMessageWorker>();
        services.AddHostedService<Maintenance.StagingCleanupWorker>();
        services.AddHostedService<Maintenance.RouletteLogCleanupService>();
        services.AddHostedService<Maintenance.ZeroingWorker>();
        services.AddHostedService<Maintenance.ChatLogCleanupWorker>();
        services.AddHostedService<Ledger.CelestialLedgerWorker>();
        services.AddHostedService<Ledger.WeeklyStatsReporter>();
        services.AddHostedService<Maintenance.RouletteResultWorker>();

        return services;
    }

    // [DEPRECATED]: 가급적 역할별로 AddCoreBotWorker 또는 AddBackgroundWorkers를 사용하세요.
    public static IServiceCollection AddWorkerRegistry(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddCoreBotWorker(configuration)
            .AddBackgroundWorkers(configuration);
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