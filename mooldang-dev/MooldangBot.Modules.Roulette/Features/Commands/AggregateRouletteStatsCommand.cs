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
            INSERT INTO LogRouletteStats (RouletteId, ItemName, TheoreticalProbability, TotalSpins, WinCount, LastUpdatedAt)
            SELECT 
                L.RouletteId,
                L.ItemName,
                IFNULL(I.Probability, 0) as TheoreticalProbability,
                COUNT(*) as TotalSpins,
                COUNT(*) as WinCount,
                NOW()
            FROM LogRouletteResults L
            LEFT JOIN FuncRouletteItems I ON L.RouletteItemId = I.Id
            WHERE L.CreatedAt >= DATE_SUB(NOW(), INTERVAL 90 DAY)
            GROUP BY L.RouletteId, L.ItemName
            ON DUPLICATE KEY UPDATE 
                TheoreticalProbability = VALUES(TheoreticalProbability),
                TotalSpins = VALUES(TotalSpins),
                WinCount = VALUES(WinCount),
                LastUpdatedAt = NOW();";;

        int affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
        logger.LogInformation("✅ [룰렛 모듈] 룰렛 감사 집계 완료 ({Count}항목 갱신)", affectedRows);
    }
}
