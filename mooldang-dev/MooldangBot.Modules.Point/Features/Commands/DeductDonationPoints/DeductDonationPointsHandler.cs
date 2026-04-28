using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.Extensions.Logging;
using Dapper;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Domain.Common;

namespace MooldangBot.Modules.Point.Features.Commands.DeductDonationPoints;

/// <summary>
/// [v7.0] 유료 재화 차감 처리기: 잔액 부족 여부를 원자적으로 검증하며 차감 시 스냅샷 기반 로그를 생성합니다.
/// </summary>
public class DeductDonationPointsHandler : IRequestHandler<DeductDonationPointsCommand, (bool Success, int CurrentBalance)>
{
    private readonly IPointDbContext _db;
    private readonly ILogger<DeductDonationPointsHandler> _logger;
    private readonly IOverlayNotificationService _notificationService;
    private readonly IIdentityCacheService _identityCache;

    public DeductDonationPointsHandler(
        IPointDbContext db,
        ILogger<DeductDonationPointsHandler> logger,
        IOverlayNotificationService notificationService,
        IIdentityCacheService identityCache)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
        _identityCache = identityCache;
    }

    public async Task<(bool Success, int CurrentBalance)> Handle(DeductDonationPointsCommand request, CancellationToken ct)
    {
        try
        {
            var streamer = await _db.CoreStreamerProfiles.AsNoTracking()
                .Select(s => new { s.Id, s.ChzzkUid })
                .FirstOrDefaultAsync(s => s.ChzzkUid == request.StreamerUid, ct);
            if (streamer == null) return (false, 0);

            // [이지스 통합]: 시청자 식별을 캐시 서비스로 일원화
            var globalId = await _identityCache.SyncGlobalViewerIdAsync(request.ViewerUid, "viewer", null, ct);
            if (globalId == 0) return (false, 0);

            var connection = _db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct);

            using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                // [오시리스의 철퇴]: 잔액이 부족하면 차감하지 않는 원자적 쿼리 (Atomic Update)
                var sql = @"
                    UPDATE func_viewer_donations 
                    SET balance = balance - @Amount, updated_at = NOW()
                    WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId 
                      AND balance >= @Amount;";

                int affectedRows = await connection.ExecuteAsync(sql, new
                {
                    StreamerId = streamer.Id,
                    GlobalId = globalId,
                    Amount = request.Amount
                }, transaction);

                var currentBalance = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT balance FROM func_viewer_donations WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                    new { StreamerId = streamer.Id, GlobalId = globalId }, transaction);

                if (affectedRows == 0) 
                {
                    _logger.LogWarning("⚠️ [후원 포인트 차감 실패] 잔액 부족: {Uid} (Req: {Amount})", request.ViewerUid, request.Amount);
                    await transaction.RollbackAsync(ct);
                    return (false, currentBalance);
                }

                // [v7.0] 감사 로그(ViewerDonationHistory) 자동 생성
                const string logSql = @"
                    INSERT INTO func_viewer_donation_histories (streamer_profile_id, global_viewer_id, platform_transaction_id, amount, balance_after, transaction_type, metadata, created_at, updated_at)
                    VALUES (@StreamerId, @GlobalId, @TxId, @Amount, @BalanceAfter, @Type, @Metadata, NOW(), NOW());";

                await connection.ExecuteAsync(logSql, new
                {
                    StreamerId = streamer.Id,
                    GlobalId = globalId,
                    TxId = Guid.NewGuid().ToString(), // 실전에서는 주문/결제 ID
                    Amount = -request.Amount,
                    BalanceAfter = currentBalance,
                    Type = "SPEND",
                    Metadata = "{\"reason\": \"Used in Command\"}",
                    CreatedAt = KstClock.Now
                }, transaction);

                await transaction.CommitAsync(ct);
                
                _ = _notificationService.NotifyPointChangedAsync(request.StreamerUid);
                return (true, currentBalance);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [DeductDonationPointsHandler] {request.ViewerUid} 차감 중 오작동: {ex.Message}");
            return (false, 0);
        }
    }
}
