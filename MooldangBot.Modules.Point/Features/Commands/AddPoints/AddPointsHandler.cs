using MooldangBot.Contracts.Point.Requests.Commands;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Dapper;
using MooldangBot.Contracts.Requests.Point.Commands;
using MooldangBot.Contracts.Security;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Point.Features.Commands.AddPoints;

public class AddPointsHandler : IRequestHandler<AddPointsCommand, (bool Success, int CurrentBalance)>
{
    private readonly IPointDbContext _db;
    private readonly ILogger<AddPointsHandler> _logger;
    private readonly IOverlayNotificationService _notificationService;

    public AddPointsHandler(
        IPointDbContext db,
        ILogger<AddPointsHandler> logger,
        IOverlayNotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, int CurrentBalance)> Handle(AddPointsCommand request, CancellationToken ct)
    {
        var columnName = request.CurrencyType == PointCurrencyType.ChatPoint ? "Points" : "DonationPoints";
        
        try
        {
            var viewerHash = Sha256Hasher.ComputeHash(request.ViewerUid);
            var streamer = await _db.StreamerProfiles.AsNoTracking().Select(s => new { s.Id, s.ChzzkUid }).FirstOrDefaultAsync(s => s.ChzzkUid == request.StreamerUid, ct);
            if (streamer == null) return (false, 0);

            var globalViewer = await _db.GlobalViewers.FirstOrDefaultAsync(g => g.ViewerUidHash == viewerHash, ct);
            if (globalViewer == null)
            {
                globalViewer = new GlobalViewer { ViewerUid = request.ViewerUid, ViewerUidHash = viewerHash, Nickname = request.Nickname ?? "" };
                _db.GlobalViewers.Add(globalViewer);
                await _db.SaveChangesAsync(ct);
            }
            else if (!string.IsNullOrEmpty(request.Nickname) && globalViewer.Nickname != request.Nickname)
            {
                globalViewer.Nickname = request.Nickname;
                globalViewer.UpdatedAt = MooldangBot.Domain.Common.KstClock.Now;
                await _db.SaveChangesAsync(ct);
            }

            var connection = _db.Database.GetDbConnection();
            var dbColumn = request.CurrencyType == PointCurrencyType.ChatPoint ? "points" : "donation_points";
            int affectedRows = 0;

            if (request.Amount < 0)
            {
                // ??녔닼?[v2.4] Atomic Banker Logic: 筌△몿而????遺용만 野꺜筌?WHERE ???곕떽?
                var updateSql = $@"
                    UPDATE view_streamer_viewers 
                    SET {dbColumn} = {dbColumn} + @Amount 
                    WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId 
                      AND {dbColumn} >= ABS(@Amount);";

                affectedRows = await connection.ExecuteAsync(new CommandDefinition(updateSql, new
                {
                    StreamerId = streamer.Id,
                    GlobalId = globalViewer.Id,
                    Amount = request.Amount
                }, cancellationToken: ct));
                
                if (affectedRows == 0) 
                {
                    _logger.LogWarning("?醫묓닔 [?????筌△몿而???쎈솭] ?遺용만 ?봔鈺? {Uid} (Req: {Amount})", request.ViewerUid, request.Amount);
                    var failResult = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                        $"SELECT {dbColumn} FROM view_streamer_viewers WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                        new { StreamerId = streamer.Id, GlobalId = globalViewer.Id }, cancellationToken: ct));
                    return (false, failResult);
                }

                // [v2.4.1] ????????걟???袁⑹읅 燁삳똻???                // FleetMetrics.PointSpentTotal.WithLabels(columnName).Inc(Math.Abs(request.Amount));
            }
            else
            {
                // ?怨룐뵲 ??뽯퓠??疫꿸퀣?덌㎗?롮쓥 Upsert ??묐뻬
                var upsertSql = $@"
                    INSERT INTO view_streamer_viewers (streamer_profile_id, global_viewer_id, points, donation_points, attendance_count, consecutive_attendance_count)
                    VALUES (@StreamerId, @GlobalId, IF(@Col='Points', @Amount, 0), IF(@Col='DonationPoints', @Amount, 0), 0, 0)
                    ON DUPLICATE KEY UPDATE 
                        {dbColumn} = {dbColumn} + @Amount;";

                affectedRows = await connection.ExecuteAsync(new CommandDefinition(upsertSql, new
                {
                    StreamerId = streamer.Id,
                    GlobalId = globalViewer.Id,
                    Amount = request.Amount,
                    Col = columnName
                }, cancellationToken: ct));

                // [v2.4.1] ??????怨룐뵲???袁⑹읅 燁삳똻???                if (request.Amount > 0)
                {
                    // FleetMetrics.PointEarnedTotal.WithLabels(columnName).Inc(request.Amount);
                }
            }

            // [v11.1] 筌ｌ뮇湲???貫?: ?怨닿탢??嚥≪뮄??疫꿸퀡以?(筌△몿而??源껊궗 ?癒?뮉 ?怨룐뵲 ??뽯퓠筌?
            var logSql = @"
                INSERT INTO log_point_transactions (streamer_profile_id, global_viewer_id, amount, type, reason, created_at)
                VALUES (@StreamerId, @GlobalId, @Amount, @Type, @Reason, NOW());";

            await connection.ExecuteAsync(new CommandDefinition(logSql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalViewer.Id,
                Amount = request.Amount,
                Type = request.Amount > 0 ? PointTransactionType.Earn : PointTransactionType.Spend,
                Reason = columnName == "DonationPoints" ? "Donation Adjustment (v2.4)" : "Command Cost (v2.4)"
            }, cancellationToken: ct));

            var result = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                $"SELECT {dbColumn} FROM view_streamer_viewers WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalViewer.Id },
                cancellationToken: ct
            ));

            _ = _notificationService.NotifyPointChangedAsync(request.StreamerUid);
            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"??[Atomic ?紐껋삏??????살첒 - {columnName}] {request.ViewerUid}: {ex.Message}");
            return (false, 0);
        }
    }
}
