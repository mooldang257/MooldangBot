using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

/// <summary>
/// [스테이징 정리 워커]: 1개월이 경과한 임시 노래 데이터를 자동으로 정리합니다.
/// </summary>
public class StagingCleanupWorker(
    IServiceProvider serviceProvider, 
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<StagingCleanupWorker> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(StagingCleanupWorker))
{
    // [지휘관 지침]: 스테이징 데이터 정리는 4시간(14,400초) 주기로 수행합니다.
    protected override int DefaultIntervalSeconds => 14400;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        _logger.LogDebug("[SongBook] 만료된 스테이징 데이터 정찰을 시작합니다.");
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 30일 경과 데이터 기준
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        // 고성능 벌크 삭제 (EF Core 7+ ExecuteDeleteAsync)
        int deletedCount = await dbContext.MasterSongStagings
            .Where(s => s.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            _logger.LogInformation("[SongBook] 만료된 스테이징 데이터 {Count}건을 삭제했습니다. (기준: {Cutoff})", deletedCount, cutoffDate);
        }
    }
}
