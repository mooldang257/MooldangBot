using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;

namespace MooldangBot.Infrastructure.Services.Background;

/// <summary>
/// [v13.1] 1개월이 경과한 스테이징 노래 데이터를 자동으로 정리하는 백그라운드 서비스입니다.
/// </summary>
public class StagingCleanupWorker(
    IServiceProvider serviceProvider, 
    ILogger<StagingCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 12시간마다 주기적으로 실행 (PeriodicTimer는 .NET 6+ 최신 기능)
        using var timer = new PeriodicTimer(TimeSpan.FromHours(12)); 

        logger.LogInformation("[SongBook] 스테이징 정리 워커가 시작되었습니다. (주환: 12시간)");

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                // 기준 시간: 현재로부터 30일 이전
                var cutoffDate = DateTime.UtcNow.AddDays(-30);

                // [v13.1] EF Core 7+ ExecuteDeleteAsync를 활용한 고성능 벌크 삭제
                int deletedCount = await dbContext.MasterSongStagings
                    .Where(s => s.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deletedCount > 0)
                {
                    logger.LogInformation("[SongBook] 만료된 스테이징 데이터 {Count}건 삭제 완료. (기준: {Cutoff})", deletedCount, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SongBook] 스테이징 데이터 정리 중 예외가 발생했습니다.");
            }
        }
    }
}
