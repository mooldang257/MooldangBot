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
        var viewer = await _db.StreamerViewers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.StreamerProfile!.ChzzkUid == streamerUid && v.GlobalViewer!.ViewerUidHash == viewerHash, ct);
        return viewer?.Points ?? 0;
    }

    public async Task<(bool Success, int CurrentPoints)> AddPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default)
    {
        return await ProcessTransactionInternalAsync(streamerUid, viewerUid, nickname, amount, "Points", ct);
    }

    public async Task<(bool Success, int CurrentBalance)> AddDonationPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default)
    {
        return await ProcessTransactionInternalAsync(streamerUid, viewerUid, nickname, amount, "DonationPoints", ct);
    }

    public async Task<(bool Success, int CurrentBalance)> DeductDonationPointsAsync(string streamerUid, string viewerUid, int amount, CancellationToken ct = default)
    {
        try
        {
            var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
            
            var streamer = await _db.StreamerProfiles.AsNoTracking().Select(s => new { s.Id, s.ChzzkUid }).FirstOrDefaultAsync(s => s.ChzzkUid == streamerUid, ct);
            if (streamer == null) return (false, 0);

            var globalId = await _db.GlobalViewers.Where(g => g.ViewerUidHash == viewerHash).Select(g => g.Id).FirstOrDefaultAsync(ct);
            if (globalId == 0) return (false, 0);

            var connection = _db.Database.GetDbConnection();
            
            // 🛡️ [오시리스의 철퇴]: 잔액이 부족하면 차감하지 않는 원자적 쿼리
            var sql = @"
                UPDATE view_streamer_viewers 
                SET DonationPoints = DonationPoints - @Amount 
                WHERE StreamerProfileId = @StreamerId AND GlobalViewerId = @GlobalId 
                  AND DonationPoints >= @Amount;";

            int affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalId,
                Amount = amount
            }, cancellationToken: ct));

            if (affectedRows == 0) return (false, await GetDonationBalanceAsync(streamerUid, viewerUid, ct));

            var currentBalance = await GetDonationBalanceAsync(streamerUid, viewerUid, ct);
            return (true, currentBalance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [DonationPoints 차감 오류] {viewerUid}: {ex.Message}");
            return (false, 0);
        }
    }

    public async Task<int> GetDonationBalanceAsync(string streamerUid, string viewerUid, CancellationToken ct = default)
    {
        var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
        var balance = await _db.StreamerViewers
            .AsNoTracking()
            .Where(v => v.StreamerProfile!.ChzzkUid == streamerUid && v.GlobalViewer!.ViewerUidHash == viewerHash)
            .Select(v => v.DonationPoints)
            .FirstOrDefaultAsync(ct);
        return balance;
    }

    private async Task<(bool Success, int ResultValue)> ProcessTransactionInternalAsync(string streamerUid, string viewerUid, string nickname, int amount, string columnName, CancellationToken ct)
    {
        try
        {
            var viewerHash = Sha256Hasher.ComputeHash(viewerUid);
            var streamer = await _db.StreamerProfiles.AsNoTracking().Select(s => new { s.Id, s.ChzzkUid }).FirstOrDefaultAsync(s => s.ChzzkUid == streamerUid, ct);
            if (streamer == null) return (false, 0);

            var globalViewer = await _db.GlobalViewers.FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);
            if (globalViewer == null)
            {
                globalViewer = new GlobalViewer { ViewerUid = viewerUid, ViewerUidHash = viewerHash, Nickname = nickname ?? "" };
                _db.GlobalViewers.Add(globalViewer);
            }
            else if (!string.IsNullOrEmpty(nickname) && globalViewer.Nickname != nickname)
            {
                globalViewer.Nickname = nickname;
                globalViewer.UpdatedAt = MooldangBot.Domain.Common.KstClock.Now;
            }
            await _db.SaveChangesAsync(ct);

            var connection = _db.Database.GetDbConnection();
            var sql = $@"
                INSERT INTO view_streamer_viewers (StreamerProfileId, GlobalViewerId, Points, DonationPoints, AttendanceCount, ConsecutiveAttendanceCount)
                VALUES (@StreamerId, @GlobalId, IF(@Col='Points', @Amount, 0), IF(@Col='DonationPoints', @Amount, 0), 0, 0)
                ON DUPLICATE KEY UPDATE 
                    {columnName} = GREATEST(0, {columnName} + @Amount);";

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalViewer.Id,
                Amount = amount,
                Col = columnName
            }, cancellationToken: ct));

            var result = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                $"SELECT {columnName} FROM view_streamer_viewers WHERE StreamerProfileId = @StreamerId AND GlobalViewerId = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalViewer.Id },
                cancellationToken: ct
            ));

            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [Dapper 트랜잭션 오류 - {columnName}] {viewerUid}: {ex.Message}");
            return (false, 0);
        }
    }
}
