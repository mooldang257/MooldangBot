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
        var connection = db.Database.GetDbConnection();

        const string sql = @"
            INSERT INTO log_point_daily_summaries (streamer_profile_id, date, total_earned, total_spent, unique_viewer_count, top_command_stats_json, last_updated_at)
            SELECT 
                T.streamer_profile_id, 
                DATE(T.created_at) as date,
                SUM(CASE WHEN T.amount > 0 THEN T.amount ELSE 0 END) as total_earned,
                SUM(CASE WHEN T.amount < 0 THEN ABS(T.amount) ELSE 0 END) as total_spent,
                COUNT(DISTINCT T.global_viewer_id) as unique_viewer_count,
                (
                    SELECT JSON_ARRAYAGG(JSON_OBJECT('keyword', keyword, 'count', cnt))
                    FROM (
                        SELECT keyword, streamer_profile_id, DATE(created_at) as log_date, COUNT(*) as cnt
                        FROM log_command_executions
                        GROUP BY streamer_profile_id, DATE(created_at), keyword
                    ) AS top_cmds
                    WHERE top_cmds.streamer_profile_id = T.streamer_profile_id 
                      AND top_cmds.log_date = DATE(T.created_at)
                    ORDER BY cnt DESC
                    LIMIT 5
                ) as top_command_stats_json,
                NOW()
            FROM log_point_transactions T
            WHERE T.created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY) 
            GROUP BY T.streamer_profile_id, DATE(T.created_at)
            ON DUPLICATE KEY UPDATE 
                total_earned = VALUES(total_earned),
                total_spent = VALUES(total_spent),
                unique_viewer_count = VALUES(unique_viewer_count),
                top_command_stats_json = VALUES(top_command_stats_json),
                last_updated_at = NOW();";

        int affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
        logger.LogInformation("✅ [포인트 모듈] 포인트 집계 완료 ({Count}일자 데이터 갱신)", affectedRows);
    }
}
