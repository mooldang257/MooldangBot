using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Dapper;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Contracts.Requests.Point.Commands;
using MooldangBot.Contracts.Security;
using MooldangBot.Domain.Entities;
using MooldangBot.Contracts.Requests.Point.Models;

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

        // [?ㅼ떆由ъ뒪??吏??: ?숈씪 ?쒖껌???ъ씤???⑹궛 諛??뺣젹 (?곕뱶??諛⑹? 諛?DB ?뺣났 媛먯냼)
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

            // 1. [?ㅼ떆由ъ뒪??湲곗뼲]: Dapper瑜??ъ슜???ㅽ듃由щ㉧ ID ?ъ쟾 留ㅽ븨 (EF 異붿쟻 諛곗젣)
            var streamerUids = sortedJobs.Select(j => j.StreamerUid).Distinct().ToArray();
            var streamerProfiles = await connection.QueryAsync<(string ChzzkUid, int Id)>(
                "SELECT chzzk_uid, id FROM core_streamer_profiles WHERE chzzk_uid IN @Uids", 
                new { Uids = streamerUids }, 
                transaction.GetDbTransaction());
            var streamerMap = streamerProfiles.ToDictionary(x => x.ChzzkUid, x => x.Id);

            // 2. [?ㅼ떆由ъ뒪???섍굅]: Dapper瑜??ъ슜??湲濡쒕쾶 ?쒖껌??ID 諛곗튂 ?섏튂
            var viewerHashes = sortedJobs.Select(j => j.ViewerHash).Distinct().ToArray();
            var globalViewers = await connection.QueryAsync<(string ViewerUidHash, int Id)>(
                "SELECT viewer_uid_hash, id FROM core_global_viewers WHERE viewer_uid_hash IN @Hashes", 
                new { Hashes = viewerHashes }, 
                transaction.GetDbTransaction());
            var viewerMap = globalViewers.ToDictionary(x => x.ViewerUidHash, x => x.Id);

            // 3. [?ㅼ떆由ъ뒪??議곌컖]: 踰뚰겕 ?뚮씪誘명꽣 ?앹꽦
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
                // [?ㅼ떆由ъ뒪???쇨꺽]: ?쒖닔 ID 湲곕컲 理쒖쟻?붾맂 踰뚰겕 ?몄꽌??(ON DUPLICATE KEY UPDATE)
                var sql = $@"
                    INSERT INTO view_streamer_viewers (streamer_profile_id, global_viewer_id, points)
                    VALUES {string.Join(",", valuesList)}
                    ON DUPLICATE KEY UPDATE points = GREATEST(0, points + VALUES(points));";

                await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction.GetDbTransaction(), cancellationToken: ct));

                // 4. [泥쒖긽???λ?]: ?몃옖??뀡 ??濡쒓렇 湲곕줉 (Dapper瑜??댁슜??????쎌엯)
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
            _logger.LogInformation("?뙄 [怨듬챸 ?낅뜲?댄듃 ?꾧껐] {Count}紐낆쓽 ?쒖껌???ъ씤?멸? Dapper 怨좎냽 寃쎈줈濡??곸옱?섏뿀?듬땲??", validJobs.Count);

            // [臾쇰찉]: ?④탳 ??쒕낫???ㅼ떆媛??낅뜲?댄듃 ?꾪뙆
            foreach (var uid in streamerUids)
            {
                _ = _notificationService.NotifyPointChangedAsync(uid);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "??[Point Dapper Bulk Update ?ㅽ뙣] {Message}", ex.Message);
            throw; 
        }
    }
}
