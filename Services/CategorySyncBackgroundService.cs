namespace MooldangAPI.Services;

public class CategorySyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CategorySyncBackgroundService> _logger;
    private DateTime? _lastRunDate;

    public CategorySyncBackgroundService(IServiceProvider serviceProvider, ILogger<CategorySyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [배치] 카테고리 동기화 스케줄러 시작 (매일 04:00 실행)");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;

            // 매일 새벽 04:00 ~ 04:01 사이에 실행 (하루 한 번만 실행되도록 체크)
            if (now.Hour == 4 && now.Minute == 0 && (_lastRunDate == null || _lastRunDate.Value.Date != now.Date))
            {
                try
                {
                    _logger.LogInformation($"⏰ [배치] 정기 카테고리 동기화 시간입니다. (현재시간: {now})");
                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<ChzzkCategorySyncService>();
                    await syncService.SyncCategoriesAsync(null, stoppingToken);
                    
                    _lastRunDate = now;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ [배치] 카테고리 동기화 중 오류 발생: {ex.Message}");
                }
            }

            // 1분마다 체크
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
