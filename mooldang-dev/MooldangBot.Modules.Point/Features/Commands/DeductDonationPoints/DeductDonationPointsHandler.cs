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
            var Streamer = await _db.TableCoreStreamerProfiles.AsNoTracking()
                .Select(s => new { s.Id, s.ChzzkUid })
                .FirstOrDefaultAsync(s => s.ChzzkUid == request.StreamerUid, ct);
            if (Streamer == null) return (false, 0);
 
            // [이지스 통합]: 시청자 식별을 캐시 서비스로 일원화
            var GlobalId = await _identityCache.SyncGlobalViewerIdAsync(request.ViewerUid, "viewer", null, ct);
            if (GlobalId == 0) return (false, 0);

            var Connection = _db.Database.GetDbConnection();
            if (Connection.State != System.Data.ConnectionState.Open) await Connection.OpenAsync(ct);
 
            using var Transaction = await Connection.BeginTransactionAsync(ct);
            try
            {
                // [오시리스의 철퇴]: 잔액이 부족하면 차감하지 않는 원자적 쿼리 (Atomic Update)
                var Sql = @"
                    UPDATE FuncViewerDonations 
                    SET Balance = Balance - @Amount, UpdatedAt = NOW()
                    WHERE StreamerProfileId = @StreamerId AND GlobalViewerId = @GlobalId 
                      AND Balance >= @Amount;";
 
                int AffectedRows = await Connection.ExecuteAsync(Sql, new
                {
                    StreamerId = Streamer.Id,
                    GlobalId = GlobalId,
                    Amount = request.Amount
                }, Transaction);
 
                var CurrentBalance = await Connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT Balance FROM FuncViewerDonations WHERE StreamerProfileId = @StreamerId AND GlobalViewerId = @GlobalId",
                    new { StreamerId = Streamer.Id, GlobalId = GlobalId }, Transaction);
 
                if (AffectedRows == 0) 
                {
                    _logger.LogWarning("⚠️ [후원 포인트 차감 실패] 잔액 부족: {Uid} (Req: {Amount})", request.ViewerUid, request.Amount);
                    await Transaction.RollbackAsync(ct);
                    return (false, CurrentBalance);
                }

                // [v7.0] 감사 로그(FuncViewerDonationHistories) 자동 생성
                const string LogSql = @"
                    INSERT INTO FuncViewerDonationHistories (StreamerProfileId, GlobalViewerId, PlatformTransactionId, Amount, BalanceAfter, TransactionType, Metadata, CreatedAt, UpdatedAt)
                    VALUES (@StreamerId, @GlobalId, @TxId, @Amount, @BalanceAfter, @Type, @Metadata, NOW(), NOW());";;
 
                await Connection.ExecuteAsync(LogSql, new
                {
                    StreamerId = Streamer.Id,
                    GlobalId = GlobalId,
                    TxId = Guid.NewGuid().ToString(), // 실전에서는 주문/결제 ID
                    Amount = -request.Amount,
                    BalanceAfter = CurrentBalance,
                    Type = "SPEND",
                    Metadata = "{\"reason\": \"Used in Command\"}",
                    CreatedAt = KstClock.Now
                }, Transaction);
 
                await Transaction.CommitAsync(ct);
                
                _ = _notificationService.NotifyPointChangedAsync(request.StreamerUid);
                return (true, CurrentBalance);
            }
            catch
            {
                await Transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, $"❌ [DeductDonationPointsHandler] {request.ViewerUid} 차감 중 오작동: {Ex.Message}");
            return (false, 0);
        }
    }
}
