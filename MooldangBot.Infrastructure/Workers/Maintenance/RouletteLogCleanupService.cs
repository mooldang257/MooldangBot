using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

/// <summary>
/// [룰렛 로그 정리 워커]: 7일이 경과한 오래된 룰렛 실행 로그를 주기적으로 삭제합니다.
/// </summary>
public class RouletteLogCleanupService(
    ILogger<RouletteLogCleanupService> logger, 
    IServiceProvider serviceProvider,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BaseHybridWorker(logger, optionsMonitor, nameof(RouletteLogCleanupService))
{
    // [지휘관 지침]: 룰렛 로그 정리는 2시간(7,200초) 주기로 수행합니다.
    protected override int DefaultIntervalSeconds => 7200;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        _logger.LogDebug("[룰렛 로그] 오래된 기록을 정찰합니다.");
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 7일 경과 데이터 기준
        var thresholdDate = KstClock.Now.AddDays(-7);
        int oldLogs = await db.RouletteLogs
            .Where(l => l.CreatedAt < thresholdDate)
            .ExecuteDeleteAsync(ct);

        if (oldLogs > 0)
        {
            _logger.LogInformation("🧹 [룰렛 로그 정리] {Count}개의 오래된 기록을 삭제했습니다. (기준: {Threshold})", oldLogs, thresholdDate);
        }
    }
}
