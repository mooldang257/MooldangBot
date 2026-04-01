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
            .FirstOrDefaultAsync(v => v.StreamerProfile!.ChzzkUid == streamerUid && v.GlobalViewer!.ViewerUidHash == viewerHash, ct);
        return viewer?.Points ?? 0;
    }

    public async Task<(bool Success, int CurrentPoints)> AddPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default)
    {
        try
        {
            var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
            
            // 1. 스트리머 ID 조회 (성능을 위해 캐시 고려 가능하나 현재는 DB 조회)
            var streamer = await _db.StreamerProfiles
                .AsNoTracking()
                .Select(s => new { s.Id, s.ChzzkUid })
                .FirstOrDefaultAsync(s => s.ChzzkUid == streamerUid, ct);
            
            if (streamer == null) return (false, 0);

            // 2. 글로벌 시청자 ID 확보 (없으면 생성)
            var globalViewer = await _db.GlobalViewers
                .FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);

            if (globalViewer == null)
            {
                globalViewer = new GlobalViewer
                {
                    ViewerUid = viewerUid,
                    ViewerUidHash = viewerHash
                };
                _db.GlobalViewers.Add(globalViewer);
                await _db.SaveChangesAsync(ct);
            }

            var connection = _db.Database.GetDbConnection();

            // 3. MariaDB Atomic Upsert: 정규화된 FK 기반 UPSERT
            var sql = @"
                INSERT INTO viewerprofiles (StreamerProfileId, GlobalViewerId, Nickname, Points, AttendanceCount, ConsecutiveAttendanceCount)
                VALUES (@StreamerId, @GlobalId, @Nickname, @Amount, 0, 0)
                ON DUPLICATE KEY UPDATE 
                    Points = GREATEST(0, Points + @Amount),
                    Nickname = CASE WHEN @Nickname != '' THEN @Nickname ELSE Nickname END;";

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalViewer.Id,
                Nickname = nickname ?? "",
                Amount = amount
            }, cancellationToken: ct));

            // 최종 포인트 조회
            var currentPoints = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                "SELECT Points FROM viewerprofiles WHERE StreamerProfileId = @StreamerId AND GlobalViewerId = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalViewer.Id },
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
