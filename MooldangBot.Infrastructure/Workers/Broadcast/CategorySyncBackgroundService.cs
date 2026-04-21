using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Features.Admin;

namespace MooldangBot.Infrastructure.Workers.Broadcast;

/// <summary>
/// [카테고리 동기화 워커]: 치지직 플랫폼의 최신 카테고리(게임 등) 목록을 로컬 DB와 동기화합니다.
/// </summary>
public class CategorySyncBackgroundService(IServiceProvider serviceProvider,
    
    ILogger<CategorySyncBackgroundService> logger, 
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(CategorySyncBackgroundService))
{
    // [지휘관 지침]: 카테고리 동기화는 5분(300초) 주기로 수행합니다.
    protected override int DefaultIntervalSeconds => 300;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ChzzkCategorySyncService>();
        
        _logger.LogInformation("📡 [카테고리 동기화] 최신 카테고리 목록을 확보합니다.");
        await syncService.SyncCategoriesAsync(ct);
    }
}
