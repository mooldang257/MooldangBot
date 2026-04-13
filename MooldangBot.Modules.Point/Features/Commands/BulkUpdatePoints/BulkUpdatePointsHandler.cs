using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Dapper;
using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.Point.Requests.Commands;
using MooldangBot.Contracts.Security;
using MooldangBot.Domain.Entities;
using MooldangBot.Contracts.Point.Requests.Models;
using MooldangBot.Contracts.Point.Interfaces;

namespace MooldangBot.Modules.Point.Features.Commands.BulkUpdatePoints;

public class BulkUpdatePointsHandler : IRequestHandler<BulkUpdatePointsCommand>
{
    private readonly IPointDbContext _db;
    private readonly ILogger<BulkUpdatePointsHandler> _logger;
    private readonly IOverlayNotificationService _notificationService;

    public BulkUpdatePointsHandler(
        IPointDbContext db,
        ILogger<BulkUpdatePointsHandler> logger,
        IOverlayNotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(BulkUpdatePointsCommand request, CancellationToken ct)
    {
        var jobList = request.Jobs.ToList();
        if (jobList.Count == 0) return;

        // [오시리스의 지혜]: 동일 시청자의 포인트 합산 및 정렬 (데드락 방지 및 DB 왕복 감소)
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

            // 1. [오시리스의 기억]: Dapper를 사용하여 스트리머 ID 사전 매핑 (EF 추적 배제)
            var streamerUids = sortedJobs.Select(j => j.StreamerUid).Distinct().ToArray();
            var streamerProfiles = await connection.QueryAsync<(string ChzzkUid, int Id)>(
                "SELECT chzzk_uid, id FROM core_streamer_profiles WHERE chzzk_uid IN @Uids", 
                new { Uids = streamerUids }, 
                transaction.GetDbTransaction());
            var streamerMap = streamerProfiles.ToDictionary(x => x.ChzzkUid, x => x.Id);

            // 2. [오시리스의 수거]: Dapper를 사용하여 글로벌 시청자 ID 배치 수취
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
                if (streamerMap.TryGetValue(job.StreamerUid, out var streamerId) &&
                    viewerMap.TryGetValue(job.ViewerHash, out var viewerId))
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
                // [오시리스의 일격]: 정수 ID 기반 최적화된 벌크 인서트 (ON DUPLICATE KEY UPDATE)
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
                    Type = (int)j.Amount > 0 ? (int)PointTransactionType.Earn : (int)PointTransactionType.Spend,
                    Reason = "Chat Resonance (10k RPS Optimized)"
                });

                await connection.ExecuteAsync(logSql, logParams, transaction: transaction.GetDbTransaction());
            }

            await transaction.CommitAsync(ct);
            _logger.LogInformation("✅ [공명 업데이트 완결] {Count}명의 시청자 포인트가 Dapper 고속 경로로 적재되었습니다.", validJobs.Count);

            // [물멍]: 전교 대시보드 실시간 업데이트 전파
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
}
