using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangAPI.Services
{
    public class RouletteLogCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RouletteLogCleanupService> _logger;

        public RouletteLogCleanupService(IServiceProvider serviceProvider, ILogger<RouletteLogCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🧹 RouletteLogCleanupService가 시작되었습니다.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // 매일 새벽 4시에 실행되도록 계산
                var now = DateTime.Now;
                var nextRunTime = now.Date.AddDays(1).AddHours(4);
                var delay = nextRunTime - now;

                _logger.LogInformation($"📅 다음 정리 예정 시각: {nextRunTime}");
                await Task.Delay(delay, stoppingToken);

                try
                {
                    await CleanupLogsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 로그 정리 중 오류 발생");
                }
            }
        }

        private async Task CleanupLogsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 기본 30일 보관 (설정에서 가져올 수 있도록 확장 가능)
            var retentionDays = 30;
            var expirationDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogInformation($"🧹 {expirationDate:yyyy-MM-dd} 이전의 완료/취소된 로그 정리를 시작합니다.");

            int totalDeleted = 0;
            bool hasMore = true;

            while (hasMore && !stoppingToken.IsCancellationRequested)
            {
                // Pending이 아닌 로그 중 오래된 것 500개씩 가져오기
                var batch = await db.RouletteLogs
                    .IgnoreQueryFilters()
                    .Where(l => l.CreatedAt < expirationDate && l.Status != RouletteLogStatus.Pending)
                    .Take(500)
                    .ToListAsync(stoppingToken);

                if (batch.Any())
                {
                    db.RouletteLogs.RemoveRange(batch);
                    await db.SaveChangesAsync(stoppingToken);
                    totalDeleted += batch.Count;
                    
                    _logger.LogInformation($"♻️ Batch 삭제 완료 ({batch.Count}개, 누적 {totalDeleted}개)");
                    await Task.Delay(100, stoppingToken); // DB 숨통 틔워주기
                }
                else
                {
                    hasMore = false;
                }
            }

            _logger.LogInformation($"✅ 로그 정리 완료 (총 {totalDeleted}개 삭제됨)");
        }
    }
}
