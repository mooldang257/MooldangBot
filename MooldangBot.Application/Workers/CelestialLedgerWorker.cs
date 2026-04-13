using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [천상의 장부 워커]: 6시간마다 방대한 로그를 요약 통계 테이블로 압축하고, 만료된 로그를 정리하는 청소기입니다.
/// (P2: 성능): Dapper를 사용하여 초고속 집계 쿼리를 수행합니다.
/// </summary>
public class CelestialLedgerWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CelestialLedgerWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);
    private const int RetentionDays = 30; // 상세 로그 보관 주기

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [천상의 장부 워커] 가동 시작 (주기: 6시간)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var pulse = scope.ServiceProvider.GetRequiredService<IPulseService>();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                pulse.ReportPulse("CelestialLedgerWorker");

                // 1. 포인트 통계 집계 (Daily Aggregation)
                await AggregatePointStatsAsync(db, stoppingToken);

                // 2. 룰렛 확률 감사 집계 (Roulette Audit)
                await AggregateRouletteStatsAsync(db, stoppingToken);

                // 3. 만료된 로그 숙청 (Retention Policy)
                await CleanupExpiredLogsAsync(db, stoppingToken);

                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [천상의 장부 워커] 집계 중 예기치 못한 오류 발생");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // 오류 시 5분 후 재시도
            }
        }
    }

    private async Task AggregatePointStatsAsync(IAppDbContext db, CancellationToken ct)
    {
        logger.LogInformation("📊 [천상의 장부] 포인트 데이터 집계 중...");
        var connection = db.Database.GetDbConnection();

        // [v11.1] Dapper 벌크 집계 쿼리: 일자별 총 획득/사용 및 시청자 수 계산
        const string sql = @"
            INSERT INTO stats_point_daily (streamer_profile_id, date, total_earned, total_spent, unique_viewer_count, top_command_stats_json, last_updated_at)
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
        logger.LogInformation("✅ [천상의 장부] 포인트 집계 완료 ({Count}일자 데이터 갱신)", affectedRows);
    }

    private async Task AggregateRouletteStatsAsync(IAppDbContext db, CancellationToken ct)
    {
        logger.LogInformation("🎰 [천상의 장부] 룰렛 확률 감사 집계 중...");
        var connection = db.Database.GetDbConnection();

        // [v11.1] 룰렛 감사 쿼리: 이론 확률과 실제 당첨 결과를 비교
        const string sql = @"
            INSERT INTO stats_roulette_audit (roulette_id, item_name, theoretical_probability, total_spins, win_count, last_updated_at)
            SELECT 
                L.roulette_id,
                L.item_name,
                IFNULL(I.probability, 0) as theoretical_probability,
                COUNT(*) as total_spins,
                COUNT(*) as win_count, -- (현재는 당첨 로그 베이스이므로 win_count = TotalSpins로 보이나, 꽝 포함 시 로직 확장 가능)
                NOW()
            FROM func_roulette_logs L
            LEFT JOIN func_roulette_items I ON L.roulette_item_id = I.id
            WHERE L.created_at >= DATE_SUB(NOW(), INTERVAL 90 DAY)
            GROUP BY L.roulette_id, L.item_name
            ON DUPLICATE KEY UPDATE 
                theoretical_probability = VALUES(theoretical_probability),
                total_spins = VALUES(total_spins),
                win_count = VALUES(win_count),
                last_updated_at = NOW();";

        int affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
        logger.LogInformation("✅ [천상의 장부] 룰렛 감사 집계 완료 ({Count}항목 갱신)", affectedRows);
    }

    private async Task CleanupExpiredLogsAsync(IAppDbContext db, CancellationToken ct)
    {
        logger.LogInformation("🧹 [천상의 장부] {RetentionDays}일이 지난 상세 로그를 숙청합니다...", RetentionDays);
        var connection = db.Database.GetDbConnection();

        // 30일이 지난 데이터 삭제 (집계 테이블이 있으므로 상세 로그는 삭제 가능)
        const string sql = "DELETE FROM log_point_transactions WHERE created_at < DATE_SUB(NOW(), INTERVAL @Days DAY) LIMIT 10000;";
        
        int totalDeleted = 0;
        int deleted;
        do
        {
            deleted = await connection.ExecuteAsync(new CommandDefinition(sql, new { Days = RetentionDays }, cancellationToken: ct));
            totalDeleted += deleted;
        } while (deleted >= 10000 && !ct.IsCancellationRequested);

        if (totalDeleted > 0)
            logger.LogWarning("⚠️ [천상의 장부] {Count}건의 오래된 포인트 로그가 숙청되었습니다.", totalDeleted);
    }
}
