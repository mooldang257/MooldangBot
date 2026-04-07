using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Dapper;
using MooldangBot.Application.Common.Security;

namespace MooldangBot.Application.Features.ChatPoints;

public class PointTransactionService : IPointTransactionService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<PointTransactionService> _logger;
    private readonly IOverlayNotificationService _notificationService;

    public PointTransactionService(
        IAppDbContext db, 
        ILogger<PointTransactionService> logger,
        IOverlayNotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task BulkUpdatePointsAsync(IEnumerable<PointJob> items, CancellationToken ct = default)
    {
        var jobList = items.ToList();
        if (jobList.Count == 0) return;

        // [오시리스의 지혜]: 동일 시청자 포인트 합산 및 정렬 (데드락 방지)
        var sortedJobs = jobList
            .GroupBy(j => (j.StreamerUid, j.ViewerUid))
            .Select(g => new { 
                g.Key.StreamerUid, 
                g.Key.ViewerUid, 
                Hash = g.Key.ViewerUid.GetHashCode(), 
                Nickname = g.First().Nickname,
                Total = g.Sum(x => x.Amount),
                ViewerHash = Sha256Hasher.ComputeHash(g.Key.ViewerUid)
            })
            .OrderBy(x => x.StreamerUid)
            .ThenBy(x => x.ViewerUid)
            .ToList();

        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var connection = _db.Database.GetDbConnection();

            // 1. [오시리스의 기억]: 스트리머 ID 사전 매핑
            var streamerUids = sortedJobs.Select(j => j.StreamerUid).Distinct().ToList();
            var streamerMap = await _db.StreamerProfiles
                .AsNoTracking()
                .Where(s => streamerUids.Contains(s.ChzzkUid))
                .ToDictionaryAsync(s => s.ChzzkUid, s => s.Id, ct);

            // 2. [오시리스의 수거]: 글로벌 시청자 ID 배치 페치
            var viewerHashes = sortedJobs.Select(j => j.ViewerHash).Distinct().ToList();
            var viewerMap = await _db.GlobalViewers
                .AsNoTracking()
                .Where(g => viewerHashes.Contains(g.ViewerUidHash))
                .ToDictionaryAsync(g => g.ViewerUidHash, g => g.Id, ct);

            // 3. [오시리스의 조각]: 벌크 파라미터 생성
            var valuesList = new List<string>();
            var parameters = new DynamicParameters();
            var validJobs = new List<dynamic>();

            for (int i = 0; i < sortedJobs.Count; i++)
            {
                var job = sortedJobs[i];
                if (streamerMap.TryGetValue(job.StreamerUid, out int streamerId) &&
                    viewerMap.TryGetValue(job.ViewerHash, out int viewerId))
                {
                    valuesList.Add($"(@S{i}, @V{i}, @A{i})");
                    parameters.Add($"S{i}", streamerId);
                    parameters.Add($"V{i}", viewerId);
                    parameters.Add($"A{i}", job.Total);
                    
                    validJobs.Add(new { StreamerId = streamerId, ViewerId = viewerId, Amount = job.Total });
                }
            }

            if (valuesList.Count > 0)
            {
                // [오시리스의 일격]: 순수 ID 기반 최적화된 벌크 인서트
                var sql = $@"
                    INSERT INTO view_streamer_viewers (StreamerProfileId, GlobalViewerId, Points)
                    VALUES {string.Join(",", valuesList)}
                    ON DUPLICATE KEY UPDATE Points = GREATEST(0, Points + VALUES(Points));";

                await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction.GetDbTransaction(), cancellationToken: ct));

                // 4. [천상의 장부]: 트랜잭션 내 로그 기록 (Atomicity 보장)
                var logSql = @"
                    INSERT INTO log_point_transactions (StreamerProfileId, GlobalViewerId, Amount, Type, Reason, CreatedAt)
                    VALUES (@StreamerId, @ViewerId, @Amount, @Type, @Reason, NOW());";
                
                var logParams = validJobs.Select(j => new {
                    StreamerId = j.StreamerId,
                    ViewerId = j.ViewerId,
                    Amount = (int)j.Amount,
                    Type = (int)j.Amount > 0 ? PointTransactionType.Earn : PointTransactionType.Spend,
                    Reason = "Chat Resonance (Atomic Batch)"
                });

                await connection.ExecuteAsync(logSql, logParams, transaction: transaction.GetDbTransaction());
            }

            await transaction.CommitAsync(ct);
            _logger.LogInformation("🌊 [공명 업데이트 완결] {Count}명의 시청자 포인트가 트랜잭션 내에서 안전하게 적재되었습니다.", validJobs.Count);

            // [물멍]: 함교 대시보드 실시간 업데이트 전파
            foreach (var uid in streamerUids)
            {
                _ = _notificationService.NotifyPointChangedAsync(uid);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "❌ [Point Bulk Update 실패] 트랜잭션 롤백됨: {Message}", ex.Message);
            throw; // 워커에서 로깅하도록 던짐
        }
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
                SET donation_points = donation_points - @Amount 
                WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId 
                  AND donation_points >= @Amount;";

            int affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalId,
                Amount = amount
            }, cancellationToken: ct));

            if (affectedRows == 0) return (false, await GetDonationBalanceAsync(streamerUid, viewerUid, ct));

            var currentBalance = await GetDonationBalanceAsync(streamerUid, viewerUid, ct);
            _ = _notificationService.NotifyPointChangedAsync(streamerUid); // [물멍]: 실시간 통계 반영
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
            var dbColumn = columnName == "Points" ? "points" : "donation_points";
            var sql = $@"
                INSERT INTO view_streamer_viewers (streamer_profile_id, global_viewer_id, points, donation_points, attendance_count, consecutive_attendance_count)
                VALUES (@StreamerId, @GlobalId, IF(@Col='Points', @Amount, 0), IF(@Col='DonationPoints', @Amount, 0), 0, 0)
                ON DUPLICATE KEY UPDATE 
                    {dbColumn} = GREATEST(0, {dbColumn} + @Amount);";

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalViewer.Id,
                Amount = amount,
                Col = columnName
            }, cancellationToken: ct));

            // [v11.1] 천상의 장부: 상세 로그 기록
            var logSql = @"
                INSERT INTO log_point_transactions (streamer_profile_id, global_viewer_id, amount, type, reason, created_at)
                VALUES (@StreamerId, @GlobalId, @Amount, @Type, @Reason, NOW());";

            await connection.ExecuteAsync(new CommandDefinition(logSql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalViewer.Id,
                Amount = amount,
                Type = amount > 0 ? PointTransactionType.Earn : PointTransactionType.Spend,
                Reason = columnName == "DonationPoints" ? "Donation Adjustment" : "Manual Adjustment"
            }, cancellationToken: ct));

            var result = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                $"SELECT {dbColumn} FROM view_streamer_viewers WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalViewer.Id },
                cancellationToken: ct
            ));

            _ = _notificationService.NotifyPointChangedAsync(streamerUid); // [물멍]: 실시간 통계 반영
            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [Dapper 트랜잭션 오류 - {columnName}] {viewerUid}: {ex.Message}");
            return (false, 0);
        }
    }
}
