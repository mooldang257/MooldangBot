using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace MooldangBot.Application.Features.ChatPoints;

public class PointTransactionService : IPointTransactionService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<PointTransactionService> _logger;

    public PointTransactionService(IAppDbContext db, ILogger<PointTransactionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> GetBalanceAsync(string streamerUid, string viewerUid, CancellationToken ct = default)
    {
        var viewer = await _db.ViewerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUid == viewerUid, ct);
        return viewer?.Points ?? 0;
    }

    public async Task<(bool Success, int CurrentPoints)> AddPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default)
    {
        try
        {
            var connection = _db.Database.GetDbConnection();

            // MariaDB Atomic Upsert: 포인트 증감 및 닉네임 동기화
            // GREATEST(0, Points + @Amount)를 통해 포인트가 음수가 되는 것을 방지
            var sql = @"
                INSERT INTO ViewerProfiles (StreamerChzzkUid, ViewerUid, Nickname, Points, AttendanceCount, ConsecutiveAttendanceCount)
                VALUES (@StreamerUid, @ViewerUid, @Nickname, @Amount, 0, 0)
                ON DUPLICATE KEY UPDATE 
                    Points = GREATEST(0, Points + @Amount),
                    Nickname = CASE WHEN @Nickname != '' THEN @Nickname ELSE Nickname END;";

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerUid = streamerUid,
                ViewerUid = viewerUid,
                Nickname = nickname ?? "",
                Amount = amount
            }, cancellationToken: ct));

            // 최종 포인트 조회
            var currentPoints = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                "SELECT Points FROM ViewerProfiles WHERE StreamerChzzkUid = @StreamerUid AND ViewerUid = @ViewerUid",
                new { StreamerUid = streamerUid, ViewerUid = viewerUid },
                cancellationToken: ct
            ));

            return (true, currentPoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [Dapper 포인트 트랜잭션 오류] {viewerUid} ({nickname}): {ex.Message}");
            return (false, 0);
        }
    }
}
