using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Point.Interfaces;
using MooldangBot.Modules.Point.Enums;
using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.Extensions.Logging;
using Dapper;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using System.Text.Json;

namespace MooldangBot.Modules.Point.Features.Commands.AddPoints;

/// <summary>
/// [v7.0] 고성능 포인트 합산 처리기: 재화 타입에 따라 Redis(Write-Back) 혹은 DB 동기 정산을 수행합니다.
/// </summary>
public class AddPointsHandler : IRequestHandler<AddPointsCommand, (bool Success, int CurrentBalance)>
{
    private readonly IPointDbContext _db;
    private readonly IPointCacheService _pointCache;
    private readonly ILogger<AddPointsHandler> _logger;
    private readonly IOverlayNotificationService _notificationService;
    private readonly IIdentityCacheService _identityCache;

    public AddPointsHandler(
        IPointDbContext db,
        IPointCacheService pointCache,
        ILogger<AddPointsHandler> logger,
        IOverlayNotificationService notificationService,
        IIdentityCacheService identityCache)
    {
        _db = db;
        _pointCache = pointCache;
        _logger = logger;
        _notificationService = notificationService;
        _identityCache = identityCache;
    }

    public async Task<(bool Success, int CurrentBalance)> Handle(AddPointsCommand request, CancellationToken ct)
    {
        try
        {
            var streamer = await _db.CoreStreamerProfiles.AsNoTracking()
                .Select(s => new { s.Id, s.ChzzkUid })
                .FirstOrDefaultAsync(s => s.ChzzkUid == request.StreamerUid, ct);
            if (streamer == null) return (false, 0);

            // 1. 글로벌 시청자 확보 (이지스 통합 캐시 활용)
            var globalViewerId = await _identityCache.SyncGlobalViewerIdAsync(request.ViewerUid, request.Nickname ?? "Unknown", null, ct);

            if (request.CurrencyType == PointCurrencyType.ChatPoint)
            {
                // [무료 포인트]: Redis를 통한 고속 Write-Back 처리
                await _pointCache.AddPointAsync(request.StreamerUid, request.ViewerUid, request.Nickname ?? "Unknown", request.Amount);
                
                // 현재 잔액은 DB값 + Redis 증분값
                var dbBalance = await _db.FuncViewerPoints.AsNoTracking()
                    .Where(v => v.StreamerProfileId == streamer.Id && v.GlobalViewerId == globalViewerId)
                    .Select(v => v.Points)
                    .FirstOrDefaultAsync(ct);
                
                var redisIncrement = await _pointCache.GetIncrementalPointAsync(request.StreamerUid, request.ViewerUid);
                
                _ = _notificationService.NotifyPointChangedAsync(request.StreamerUid);
                return (true, dbBalance + redisIncrement);
            }
            else
            {
                // [유료/수동 재화]: MariaDB 동기 업데이트
                var connection = _db.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct);

                using var transaction = await connection.BeginTransactionAsync(ct);
                try
                {
                    // [1순위: Identity First] 시청자 관계 및 닉네임, 마지막 활동 시간 등록
                    const string relationUpsertSql = @"
                        INSERT INTO core_viewer_relations (streamer_profile_id, global_viewer_id, is_active, is_deleted, attendance_count, consecutive_attendance_count, first_visit_at, last_chat_at, created_at, updated_at)
                        VALUES (@StreamerId, @GlobalId, 1, 0, 0, 0, NOW(), NOW(), NOW(), NOW())
                        ON DUPLICATE KEY UPDATE 
                            last_chat_at = NOW(),
                            updated_at = NOW();";

                    await connection.ExecuteAsync(relationUpsertSql, new { StreamerId = streamer.Id, GlobalId = globalViewerId }, transaction);

                    // [2순위: 포인트/재화 정산]
                    int totalIncrement = request.AccumulateTotal && request.Amount > 0 ? request.Amount : 0;

                    const string upsertSql = @"
                        INSERT INTO func_viewer_donations (streamer_profile_id, global_viewer_id, balance, total_donated, created_at, updated_at)
                        VALUES (@StreamerId, @GlobalId, @Amount, @TotalIncrement, NOW(), NOW())
                        ON DUPLICATE KEY UPDATE 
                            balance = balance + @Amount,
                            total_donated = total_donated + @TotalIncrement,
                            updated_at = NOW();";

                    await connection.ExecuteAsync(upsertSql, new 
                    { 
                        StreamerId = streamer.Id, 
                        GlobalId = globalViewerId, 
                        Amount = request.Amount,
                        TotalIncrement = totalIncrement
                    }, transaction);

                    // 2. 최종 잔액 조회 (스냅샷 생성용)
                    var currentBalance = await connection.QueryFirstOrDefaultAsync<int>(
                        "SELECT balance FROM func_viewer_donations WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                        new { StreamerId = streamer.Id, GlobalId = globalViewerId }, transaction);

                    // 3. 감사 로그(ViewerDonationHistory) 기록
                    const string logSql = @"
                        INSERT INTO func_viewer_donation_histories (streamer_profile_id, global_viewer_id, platform_transaction_id, amount, balance_after, transaction_type, metadata, created_at, updated_at)
                        VALUES (@StreamerId, @GlobalId, @TxId, @Amount, @BalanceAfter, @Type, @Metadata, NOW(), NOW());";

                    await connection.ExecuteAsync(logSql, new
                    {
                        StreamerId = streamer.Id,
                        GlobalId = globalViewerId,
                        TxId = request.PlatformTransactionId ?? $"ADJ-{Guid.NewGuid():N}", // [v7.1] 전송된 SafeEventId 사용
                        Amount = request.Amount,
                        BalanceAfter = currentBalance,
                        Type = request.Amount >= 0 ? "DONATION" : "DEDUCTION",
                        Metadata = JsonSerializer.Serialize(new { 
                            source = request.PlatformTransactionId != null ? "chzzk_gateway" : "manual",
                            request_amount = request.Amount 
                        }),
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [AddPointsHandler] {request.ViewerUid} 정산 중 오작동: {ex.Message}");
            return (false, 0);
        }
    }
}
