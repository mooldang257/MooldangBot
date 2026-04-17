using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Features.Admin;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace MooldangBot.Infrastructure.Workers.Broadcast;

public class CategorySyncBackgroundService(
    ILogger<CategorySyncBackgroundService> logger, 
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BackgroundService
{
    private const string WorkerName = nameof(CategorySyncBackgroundService);

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [CategorySyncBackgroundService] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = CurrentSettings;
            if (!settings.IsEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ChzzkCategorySyncService>();
                await syncService.SyncCategoriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [카테고리 동기화] 오류 발생");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
        }
    }
}
