using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Modules.Roulette.Features.Commands;

/// <summary>
/// [룰렛 집계]: 룰렛의 이론 확률과 실제 당첨 결과를 비교 분석하여 요약합니다.
/// </summary>
public record AggregateRouletteStatsCommand : IRequest;

public class AggregateRouletteStatsCommandHandler(
    IAppDbContext db,
    ILogger<AggregateRouletteStatsCommandHandler> logger) : IRequestHandler<AggregateRouletteStatsCommand>
{
    public async Task Handle(AggregateRouletteStatsCommand request, CancellationToken ct)
    {
        logger.LogInformation("🎰 [룰렛 모듈] 룰렛 확률 감사 집계 중...");
        var connection = db.Database.GetDbConnection();

        const string sql = @"
            INSERT INTO log_roulette_stats (roulette_id, item_name, theoretical_probability, total_spins, win_count, last_updated_at)
            SELECT 
                L.roulette_id,
                L.item_name,
                IFNULL(I.probability, 0) as theoretical_probability,
                COUNT(*) as total_spins,
                COUNT(*) as win_count,
                NOW()
            FROM log_roulette_results L
            LEFT JOIN func_roulette_items I ON L.roulette_item_id = I.id
            WHERE L.created_at >= DATE_SUB(NOW(), INTERVAL 90 DAY)
            GROUP BY L.roulette_id, L.item_name
            ON DUPLICATE KEY UPDATE 
                theoretical_probability = VALUES(theoretical_probability),
                total_spins = VALUES(total_spins),
                win_count = VALUES(win_count),
                last_updated_at = NOW();";

        int affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
        logger.LogInformation("✅ [룰렛 모듈] 룰렛 감사 집계 완료 ({Count}항목 갱신)", affectedRows);
    }
}
