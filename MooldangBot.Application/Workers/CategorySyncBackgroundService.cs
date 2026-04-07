using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Features.Admin;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Workers;

public class CategorySyncBackgroundService : BackgroundService
{
    private readonly ILogger<CategorySyncBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CategorySyncBackgroundService(ILogger<CategorySyncBackgroundService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [카테고리 동기화 백그라운드 서비스] 가동 중...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ChzzkCategorySyncService>();
                await syncService.SyncCategoriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [카테고리 동기화] 오류 발생");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
