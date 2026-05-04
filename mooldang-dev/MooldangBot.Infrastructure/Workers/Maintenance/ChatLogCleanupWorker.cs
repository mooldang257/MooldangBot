using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Infrastructure.Workers.Maintenance;

/// <summary>
/// [채팅 로그 호상인]: 생성된 지 90일이 지난 채팅 로그 다건을 주기적으로 정리합니다.
/// </summary>
public class ChatLogCleanupWorker(
    IServiceProvider serviceProvider,
    ILogger<ChatLogCleanupWorker> logger,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(ChatLogCleanupWorker))
{
    protected override bool RequiresDistributedLock => true;
    
    // 하루 1번 실행 (86400초)
    protected override int DefaultIntervalSeconds => 86400;

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        _logger.LogInformation("🧹 [ChatLogCleanup] 90일 이상 경과한 채팅 로그 정리를 시작합니다.");
 
        using var Scope = _serviceProvider.CreateScope();
        var Db = Scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var Connection = Db.Database.GetDbConnection();
        if (Connection.State != System.Data.ConnectionState.Open)
        {
            await Connection.OpenAsync(ct);
        }
 
        try
        {
            const string Sql = @"
                DELETE FROM LogChatInteractions 
                WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 90 DAY)";
 
            var DeletedCount = await Connection.ExecuteAsync(Sql);
            
            if (DeletedCount > 0)
            {
                _logger.LogInformation("✅ [ChatLogCleanup] {Count}개의 오래된 채팅 로그를 정리했습니다.", DeletedCount);
            }
            else
            {
                _logger.LogInformation("✅ [ChatLogCleanup] 정리할 채팅 로그가 없습니다.");
            }
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, "🚨 [ChatLogCleanup] 채팅 로그 정리 중 오류 발생!");
        }
    }
}
