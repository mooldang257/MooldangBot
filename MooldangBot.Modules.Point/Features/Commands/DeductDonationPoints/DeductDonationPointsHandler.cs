using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.Extensions.Logging;
using Dapper;
using MooldangBot.Contracts.Security;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Contracts.Point.Requests.Commands;
using MooldangBot.Contracts.Commands.Interfaces;

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
            
            // 🔒 [오시리스의 철퇴]: 잔액이 부족하면 차감하지 않는 원자적 쿼리
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
                _logger.LogWarning("⚠️ [후원 포인트 차감 실패] 잔액 부족: {Uid} (Req: {Amount})", request.ViewerUid, request.Amount);
                return (false, currentBalance);
            }

            _ = _notificationService.NotifyPointChangedAsync(request.StreamerUid); // [물멍]: 실시간 통계 반영
            return (true, currentBalance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [DonationPoints 차감 오류] {request.ViewerUid}: {ex.Message}");
            return (false, 0);
        }
    }
}
