using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Modules.Point.Features.Commands;

/// <summary>
/// [로그 숙청]: 보관 주기가 지난 상세 포인트 트랜잭션 로그를 삭제합니다.
/// </summary>
/// <param name="RetentionDays">보관 일수 (기본 30일)</param>
public record CleanupExpiredLogsCommand(int RetentionDays = 30) : IRequest;

public class CleanupExpiredLogsCommandHandler(
    IAppDbContext db,
    ILogger<CleanupExpiredLogsCommandHandler> logger) : IRequestHandler<CleanupExpiredLogsCommand>
{
    public async Task Handle(CleanupExpiredLogsCommand request, CancellationToken ct)
    {
        logger.LogInformation("🧹 [포인트 모듈] {RetentionDays}일이 지난 상세 로그를 숙청합니다...", request.RetentionDays);
        var Connection = db.Database.GetDbConnection();
 
        const string Sql = "DELETE FROM LogPointTransactions WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL @Days DAY) LIMIT 10000;";
        
        int TotalDeleted = 0;
        int Deleted;
        do
        {
            Deleted = await Connection.ExecuteAsync(new CommandDefinition(Sql, new { Days = request.RetentionDays }, cancellationToken: ct));
            TotalDeleted += Deleted;
        } while (Deleted >= 10000 && !ct.IsCancellationRequested);
 
        if (TotalDeleted > 0)
            logger.LogWarning("⚠️ [포인트 모듈] {Count}건의 오래된 포인트 로그가 숙청되었습니다.", TotalDeleted);
    }
}
