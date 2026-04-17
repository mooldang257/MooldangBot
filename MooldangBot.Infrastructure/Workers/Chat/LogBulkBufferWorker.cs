using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options;
using MooldangBot.Contracts.Chzzk;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Application.Services.Philosophy;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Infrastructure.Workers.Chat;

/// <summary>
/// [기록관의 수레]: 버퍼에 쌓인 대량의 로그를 EFCore.BulkExtensions를 강화하여 DB에 일괄 저장합니다.
/// 서비스 종료 시(Graceful Shutdown)에도 남은 데이터를 모두 쏟아냅니다.
/// </summary>
public class LogBulkBufferWorker(
    LogBulkBuffer buffer,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor, // [수정] Named Options 패턴 적용
    ILogger<LogBulkBufferWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(LogBulkBufferWorker);

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [LogBulkBufferWorker] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

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
                await FlushAsync();
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("👋 [기록관의 수레] 잔여 데이터를 모두 쏟아내고 안전하게 종료합니다.");
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

            var bulkConfig = new BulkConfig 
            { 
                BatchSize = 1000
            };

            if (vibrationLogs.Count > 0)
            {
                logger.LogInformation("[기록관의 수레] {Count}개의 진동 로그를 벌크 저장합니다.", vibrationLogs.Count);
                await dbContext.BulkInsertAsync(vibrationLogs, bulkConfig);
            }

            if (scenarios.Count > 0)
            {
                logger.LogInformation("[기록관의 수레] {Count}개의 시나리오 로그를 벌크 저장합니다.", scenarios.Count);
                await dbContext.BulkInsertAsync(scenarios, bulkConfig);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[기록관의 수레] 벌크 인서트 실패");
        }
    }
}
