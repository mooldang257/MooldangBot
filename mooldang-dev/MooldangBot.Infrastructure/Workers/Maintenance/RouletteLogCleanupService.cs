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
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(RouletteLogCleanupService))
{
    // [지휘관 지침]: 룰렛 로그 정리는 24시간(86,400초) 주기로 수행합니다.
    protected override int DefaultIntervalSeconds => 86400;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        _logger.LogDebug("[룰렛 로그] 오래된 기록을 정찰합니다.");
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 90일 경과 데이터 중 미수행(Pending) 상태가 아닌 것만 삭제
        var thresholdDate = KstClock.Now.AddDays(-90);
        int oldLogs = await db.FuncRouletteLogs
            .Where(l => l.CreatedAt < thresholdDate && l.Status != MooldangBot.Domain.Entities.RouletteLogStatus.Pending)
            .ExecuteDeleteAsync(ct);

        if (oldLogs > 0)
        {
            _logger.LogInformation("🧹 [룰렛 로그 정리] {Count}개의 오래된 기록을 삭제했습니다. (기준: {Threshold})", oldLogs, thresholdDate);
        }
    }
}
