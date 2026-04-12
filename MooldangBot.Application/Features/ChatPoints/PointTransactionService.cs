using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Dapper;
using MooldangBot.Application.Common.Security;
using MooldangBot.Application.Common.Metrics;

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

        // [오시리스의 지혜]: 동일 시청자 포인트 합산 및 정렬 (데드락 방지 및 DB 왕복 감소)
        var sortedJobs = jobList
            .GroupBy(j => (j.StreamerUid, j.ViewerUid))
            .Select(g => new { 
                g.Key.StreamerUid, 
                g.Key.ViewerUid, 
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

            // 1. [오시리스의 기억]: Dapper를 사용한 스트리머 ID 사전 매핑 (EF 추적 배제)
            var streamerUids = sortedJobs.Select(j => j.StreamerUid).Distinct().ToArray();
            var streamerProfiles = await connection.QueryAsync<(string ChzzkUid, int Id)>(
                "SELECT chzzk_uid, id FROM core_streamer_profiles WHERE chzzk_uid IN @Uids", 
                new { Uids = streamerUids }, 
                transaction.GetDbTransaction());
            var streamerMap = streamerProfiles.ToDictionary(x => x.ChzzkUid, x => x.Id);

            // 2. [오시리스의 수거]: Dapper를 사용한 글로벌 시청자 ID 배치 페치
            var viewerHashes = sortedJobs.Select(j => j.ViewerHash).Distinct().ToArray();
            var globalViewers = await connection.QueryAsync<(string ViewerUidHash, int Id)>(
                "SELECT viewer_uid_hash, id FROM core_global_viewers WHERE viewer_uid_hash IN @Hashes", 
                new { Hashes = viewerHashes }, 
                transaction.GetDbTransaction());
            var viewerMap = globalViewers.ToDictionary(x => x.ViewerUidHash, x => x.Id);

            // 3. [오시리스의 조각]: 벌크 파라미터 생성
            var valuesList = new List<string>(sortedJobs.Count);
            var parameters = new DynamicParameters();
            var validJobs = new List<dynamic>(sortedJobs.Count);

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
                // [오시리스의 일격]: 순수 ID 기반 최적화된 벌크 인서트 (ON DUPLICATE KEY UPDATE)
                var sql = $@"
                    INSERT INTO view_streamer_viewers (streamer_profile_id, global_viewer_id, points)
                    VALUES {string.Join(",", valuesList)}
                    ON DUPLICATE KEY UPDATE points = GREATEST(0, points + VALUES(points));";

                await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction.GetDbTransaction(), cancellationToken: ct));

                // 4. [천상의 장부]: 트랜잭션 내 로그 기록 (Dapper를 이용한 대량 삽입)
                var logSql = @"
                    INSERT INTO log_point_transactions (streamer_profile_id, global_viewer_id, amount, type, reason, created_at)
                    VALUES (@StreamerId, @ViewerId, @Amount, @Type, @Reason, NOW());";
                
                var logParams = validJobs.Select(j => new {
                    StreamerId = j.StreamerId,
                    ViewerId = j.ViewerId,
                    Amount = (int)j.Amount,
                    Type = (int)j.Amount > 0 ? PointTransactionType.Earn : PointTransactionType.Spend,
                    Reason = "Chat Resonance (10k RPS Optimized)"
                });

                await connection.ExecuteAsync(logSql, logParams, transaction: transaction.GetDbTransaction());
            }

            await transaction.CommitAsync(ct);
            _logger.LogInformation("🌊 [공명 업데이트 완결] {Count}명의 시청자 포인트가 Dapper 고속 경로로 적재되었습니다.", validJobs.Count);

            // [물멍]: 함교 대시보드 실시간 업데이트 전파
            foreach (var uid in streamerUids)
            {
                _ = _notificationService.NotifyPointChangedAsync(uid);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "❌ [Point Dapper Bulk Update 실패] {Message}", ex.Message);
            throw; 
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
                await _db.SaveChangesAsync(ct);
            }
            else if (!string.IsNullOrEmpty(nickname) && globalViewer.Nickname != nickname)
            {
                globalViewer.Nickname = nickname;
                globalViewer.UpdatedAt = MooldangBot.Domain.Common.KstClock.Now;
                await _db.SaveChangesAsync(ct);
            }

            var connection = _db.Database.GetDbConnection();
            var dbColumn = columnName == "Points" ? "points" : "donation_points";
            int affectedRows = 0;

            if (amount < 0)
            {
                // 🛡️ [v2.4] Atomic Banker Logic: 차감 시 잔액 검증 WHERE 절 추가
                var updateSql = $@"
                    UPDATE view_streamer_viewers 
                    SET {dbColumn} = {dbColumn} + @Amount 
                    WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId 
                      AND {dbColumn} >= ABS(@Amount);";

                affectedRows = await connection.ExecuteAsync(new CommandDefinition(updateSql, new
                {
                    StreamerId = streamer.Id,
                    GlobalId = globalViewer.Id,
                    Amount = amount
                }, cancellationToken: ct));
                
                if (affectedRows == 0) 
                {
                    _logger.LogWarning("⚠️ [포인적 차감 실패] 잔액 부족: {Uid} (Req: {Amount})", viewerUid, amount);
                    var failResult = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                        $"SELECT {dbColumn} FROM view_streamer_viewers WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                        new { StreamerId = streamer.Id, GlobalId = globalViewer.Id }, cancellationToken: ct));
                    return (false, failResult);
                }

                // [v2.4.1] 포인트 소모량 누적 카운팅
                FleetMetrics.PointSpentTotal.WithLabels(columnName).Inc(Math.Abs(amount));
            }
            else
            {
                // 적립 시에는 기존처럼 Upsert 수행
                var upsertSql = $@"
                    INSERT INTO view_streamer_viewers (streamer_profile_id, global_viewer_id, points, donation_points, attendance_count, consecutive_attendance_count)
                    VALUES (@StreamerId, @GlobalId, IF(@Col='Points', @Amount, 0), IF(@Col='DonationPoints', @Amount, 0), 0, 0)
                    ON DUPLICATE KEY UPDATE 
                        {dbColumn} = {dbColumn} + @Amount;";

                affectedRows = await connection.ExecuteAsync(new CommandDefinition(upsertSql, new
                {
                    StreamerId = streamer.Id,
                    GlobalId = globalViewer.Id,
                    Amount = amount,
                    Col = columnName
                }, cancellationToken: ct));

                // [v2.4.1] 포인트 적립량 누적 카운팅
                if (amount > 0)
                {
                    FleetMetrics.PointEarnedTotal.WithLabels(columnName).Inc(amount);
                }
            }

            // [v11.1] 천상의 장부: 상거래 로그 기록 (차감 성공 또는 적립 시에만)
            var logSql = @"
                INSERT INTO log_point_transactions (streamer_profile_id, global_viewer_id, amount, type, reason, created_at)
                VALUES (@StreamerId, @GlobalId, @Amount, @Type, @Reason, NOW());";

            await connection.ExecuteAsync(new CommandDefinition(logSql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalViewer.Id,
                Amount = amount,
                Type = amount > 0 ? PointTransactionType.Earn : PointTransactionType.Spend,
                Reason = columnName == "DonationPoints" ? "Donation Adjustment (v2.4)" : "Command Cost (v2.4)"
            }, cancellationToken: ct));

            var result = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                $"SELECT {dbColumn} FROM view_streamer_viewers WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalViewer.Id },
                cancellationToken: ct
            ));

            _ = _notificationService.NotifyPointChangedAsync(streamerUid);
            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [Atomic 트랜잭션 오류 - {columnName}] {viewerUid}: {ex.Message}");
            return (false, 0);
        }
    }
}
