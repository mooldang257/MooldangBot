using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Dapper;
using MooldangBot.Application.Common.Security;

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
        var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
        var viewer = await _db.ViewerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUidHash == viewerHash, ct);
        return viewer?.Points ?? 0;
    }

    public async Task<(bool Success, int CurrentPoints)> AddPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default)
    {
        try
        {
            var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
            var connection = _db.Database.GetDbConnection();

            // MariaDB Atomic Upsert: 포인트 증감 및 닉네임 동기화
            // [v4.0] Search Hash 전략: Index(StreamerChzzkUid, ViewerUidHash) 기반 UPSERT
            var sql = @"
                INSERT INTO ViewerProfiles (StreamerChzzkUid, ViewerUid, ViewerUidHash, Nickname, Points, AttendanceCount, ConsecutiveAttendanceCount)
                VALUES (@StreamerUid, @ViewerUid, @ViewerUidHash, @Nickname, @Amount, 0, 0)
                ON DUPLICATE KEY UPDATE 
                    Points = GREATEST(0, Points + @Amount),
                    Nickname = CASE WHEN @Nickname != '' THEN @Nickname ELSE Nickname END;";

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerUid = streamerUid,
                ViewerUid = viewerUid, // AppDbContext의 Converter가 암호화 처리함
                ViewerUidHash = viewerHash,
                Nickname = nickname ?? "",
                Amount = amount
            }, cancellationToken: ct));

            // 최종 포인트 조회
            var currentPoints = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                "SELECT Points FROM ViewerProfiles WHERE StreamerChzzkUid = @StreamerUid AND ViewerUidHash = @ViewerUidHash",
                new { StreamerUid = streamerUid, ViewerUidHash = viewerHash },
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
