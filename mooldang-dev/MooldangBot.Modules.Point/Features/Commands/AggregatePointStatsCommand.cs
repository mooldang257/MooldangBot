using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Modules.Point.Features.Commands;

/// <summary>
/// [포인트 집계]: 일자별 총 획득/사용 포인트 및 고유 시청자 수를 집계 테이블로 압축합니다.
/// </summary>
public record AggregatePointStatsCommand : IRequest;

public class AggregatePointStatsCommandHandler(
    IAppDbContext db,
    ILogger<AggregatePointStatsCommandHandler> logger) : IRequestHandler<AggregatePointStatsCommand>
{
    public async Task Handle(AggregatePointStatsCommand request, CancellationToken ct)
    {
        logger.LogInformation("📊 [포인트 모듈] 포인트 통계 데이터 집계 중...");
        var Connection = db.Database.GetDbConnection();
 
        const string Sql = @"
            INSERT INTO LogPointDailySummaries (StreamerProfileId, Date, TotalEarned, TotalSpent, UniqueViewerCount, TopCommandStatsJson, LastUpdatedAt)
            SELECT 
                T.StreamerProfileId, 
                DATE(T.CreatedAt) as Date,
                SUM(CASE WHEN T.Amount > 0 THEN T.Amount ELSE 0 END) as TotalEarned,
                SUM(CASE WHEN T.Amount < 0 THEN ABS(T.Amount) ELSE 0 END) as TotalSpent,
                COUNT(DISTINCT T.GlobalViewerId) as UniqueViewerCount,
                (
                    SELECT JSON_ARRAYAGG(JSON_OBJECT('Keyword', Keyword, 'Count', Cnt))
                    FROM (
                        SELECT Keyword, StreamerProfileId, DATE(CreatedAt) as LogDate, COUNT(*) as Cnt
                        FROM LogCommandExecutions
                        GROUP BY StreamerProfileId, DATE(CreatedAt), Keyword
                    ) AS TopCmds
                    WHERE TopCmds.StreamerProfileId = T.StreamerProfileId 
                      AND TopCmds.LogDate = DATE(T.CreatedAt)
                    ORDER BY Cnt DESC
                    LIMIT 5
                ) as TopCommandStatsJson,
                NOW()
            FROM LogPointTransactions T
            WHERE T.CreatedAt >= DATE_SUB(NOW(), INTERVAL 7 DAY) 
            GROUP BY T.StreamerProfileId, DATE(T.CreatedAt)
            ON DUPLICATE KEY UPDATE 
                TotalEarned = VALUES(TotalEarned),
                TotalSpent = VALUES(TotalSpent),
                UniqueViewerCount = VALUES(UniqueViewerCount),
                TopCommandStatsJson = VALUES(TopCommandStatsJson),
                LastUpdatedAt = NOW();";
 
        int AffectedRows = await Connection.ExecuteAsync(new CommandDefinition(Sql, cancellationToken: ct));
        logger.LogInformation("✅ [포인트 모듈] 포인트 집계 완료 ({Count}일자 데이터 갱신)", AffectedRows);
    }
}
