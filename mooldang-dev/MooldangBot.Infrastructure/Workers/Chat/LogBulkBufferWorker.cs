using MooldangBot.Foundation.Services;
using MooldangBot.Foundation.Workers;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Application.Services.Philosophy;

namespace MooldangBot.Infrastructure.Workers.Chat;

/// <summary>
/// [기록관의 수레]: 버퍼에 쌓인 대량의 로그를 EFCore.BulkExtensions를 강화하여 DB에 일괄 저장합니다.
/// </summary>
public class LogBulkBufferWorker(IServiceProvider serviceProvider,
    
    LogBulkBuffer buffer,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<LogBulkBufferWorker> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(LogBulkBufferWorker))
{
    protected override int DefaultIntervalSeconds => 1;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        await FlushAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("[기록관의 수레] 시스템 종료 감지. 잔여 로그를 벌크 인서트합니다.");
        await FlushAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync()
    {
        var VibrationLogs = buffer.DrainVibrationLogs();
        var Scenarios = buffer.DrainScenarios();
 
        if (VibrationLogs.Count == 0 && Scenarios.Count == 0) return;
 
        try
        {
            using var Scope = scopeFactory.CreateScope();
            var Db = Scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var DbContext = (DbContext)Db;
 
            var BulkConfig = new BulkConfig 
            { 
                BatchSize = 1000
            };
 
            if (VibrationLogs.Count > 0)
            {
                _logger.LogInformation("[기록관의 수레] {Count}개의 진동 로그를 벌크 저장합니다.", VibrationLogs.Count);
                await DbContext.BulkInsertAsync(VibrationLogs, BulkConfig);
            }
 
            if (Scenarios.Count > 0)
            {
                _logger.LogInformation("[기록관의 수레] {Count}개의 시나리오 로그를 벌크 저장합니다.", Scenarios.Count);
                await DbContext.BulkInsertAsync(Scenarios, BulkConfig);
            }
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, "[기록관의 수레] 벌크 인서트 실패");
        }
    }
}
