using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Dapper;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Contracts.Requests.Point.Commands;
using MooldangBot.Contracts.Security;

namespace MooldangBot.Modules.Point.Features.Commands.DeductDonationPoints;

public class DeductDonationPointsHandler : IRequestHandler<DeductDonationPointsCommand, (bool Success, int CurrentBalance)>
{
    private readonly IPointDbContext _db;
    private readonly ILogger<DeductDonationPointsHandler> _logger;
    private readonly IOverlayNotificationService _notificationService;

    public DeductDonationPointsHandler(
        IPointDbContext db,
        ILogger<DeductDonationPointsHandler> logger,
        IOverlayNotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, int CurrentBalance)> Handle(DeductDonationPointsCommand request, CancellationToken ct)
    {
        try
        {
            var viewerHash = Sha256Hasher.ComputeHash(request.ViewerUid);
            
            var streamer = await _db.StreamerProfiles.AsNoTracking().Select(s => new { s.Id, s.ChzzkUid }).FirstOrDefaultAsync(s => s.ChzzkUid == request.StreamerUid, ct);
            if (streamer == null) return (false, 0);

            var globalId = await _db.GlobalViewers.Where(g => g.ViewerUidHash == viewerHash).Select(g => g.Id).FirstOrDefaultAsync(ct);
            if (globalId == 0) return (false, 0);

            var connection = _db.Database.GetDbConnection();
            
            // ?썳截?[?ㅼ떆由ъ뒪??泥좏눜]: ?붿븸??遺議깊븯硫?李④컧?섏? ?딅뒗 ?먯옄??荑쇰━
            var sql = @"
                UPDATE view_streamer_viewers 
                SET donation_points = donation_points - @Amount 
                WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId 
                  AND donation_points >= @Amount;";

            int affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                StreamerId = streamer.Id,
                GlobalId = globalId,
                Amount = request.Amount
            }, cancellationToken: ct));

            var currentBalance = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                "SELECT donation_points FROM view_streamer_viewers WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalId }, cancellationToken: ct));

            if (affectedRows == 0) 
            {
                _logger.LogWarning("?좑툘 [?꾩썝 ?ъ씤??李④컧 ?ㅽ뙣] ?붿븸 遺議? {Uid} (Req: {Amount})", request.ViewerUid, request.Amount);
                return (false, currentBalance);
            }

            _ = _notificationService.NotifyPointChangedAsync(request.StreamerUid); // [臾쇰찉]: ?ㅼ떆媛??듦퀎 諛섏쁺
            return (true, currentBalance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"??[DonationPoints 李④컧 ?ㅻ쪟] {request.ViewerUid}: {ex.Message}");
            return (false, 0);
        }
    }
}
