using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [기록관의 수레]: 버퍼에 쌓인 대량의 로그를 EFCore.BulkExtensions를 강화하여 DB에 일괄 저장합니다.
/// 서비스 종료 시(Graceful Shutdown)에도 남은 데이터를 모두 쏟아냅니다.
/// </summary>
public class LogBulkBufferWorker(
    ILogBulkBuffer buffer,
    IServiceScopeFactory scopeFactory,
    ILogger<LogBulkBufferWorker> logger) : BackgroundService
{
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[기록관의 수레] 벌크 로그 라이터가 가동되었습니다. (주기: 10초)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_flushInterval, stoppingToken);
                await FlushAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[기록관의 수레] 주기적 플러시 중 오류 발생");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("[기록관의 수레] 시스템 종료 감지. 잔여 로그를 벌크 인서트합니다.");
        await FlushAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync()
    {
        var vibrationLogs = buffer.DrainVibrationLogs();
        var scenarios = buffer.DrainScenarios();

        if (vibrationLogs.Count == 0 && scenarios.Count == 0) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var dbContext = (DbContext)db; // BulkInsertAsync 호출을 위해 캐스팅

            if (vibrationLogs.Count > 0)
            {
                logger.LogInformation($"[기록관의 수레] {vibrationLogs.Count}개의 진동 로그를 벌크 저장합니다.");
                await dbContext.BulkInsertAsync(vibrationLogs);
            }

            if (scenarios.Count > 0)
            {
                logger.LogInformation($"[기록관의 수레] {scenarios.Count}개의 시나리오 로그를 벌크 저장합니다.");
                await dbContext.BulkInsertAsync(scenarios);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[기록관의 수레] 벌크 인서트 실패");
        }
    }
}
