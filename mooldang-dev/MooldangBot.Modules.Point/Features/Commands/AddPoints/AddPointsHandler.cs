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
            var Streamer = await _db.TableCoreStreamerProfiles.AsNoTracking()
                .Select(s => new { s.Id, s.ChzzkUid })
                .FirstOrDefaultAsync(s => s.ChzzkUid == request.StreamerUid, ct);
            if (Streamer == null) return (false, 0);
 
            // 1. 글로벌 시청자 확보 (이지스 통합 캐시 활용)
            var GlobalViewerId = await _identityCache.SyncGlobalViewerIdAsync(request.ViewerUid, request.Nickname ?? "Unknown", null, ct);

            if (request.CurrencyType == PointCurrencyType.ChatPoint)
            {
                // [무료 포인트]: Redis를 통한 고속 Write-Back 처리
                await _pointCache.AddPointAsync(request.StreamerUid, request.ViewerUid, request.Nickname ?? "Unknown", request.Amount);
                
                // 현재 잔액은 DB값 + Redis 증분값
                var DbBalance = await _db.TableFuncViewerPoints.AsNoTracking()
                    .Where(v => v.StreamerProfileId == Streamer.Id && v.GlobalViewerId == GlobalViewerId)
                    .Select(v => v.Points)
                    .FirstOrDefaultAsync(ct);
                
                var RedisIncrement = await _pointCache.GetIncrementalPointAsync(request.StreamerUid, request.ViewerUid);
                
                _ = _notificationService.NotifyPointChangedAsync(request.StreamerUid);
                return (true, DbBalance + RedisIncrement);
            }
            else
            {
                // [유료/수동 재화]: MariaDB 동기 업데이트
                var Connection = _db.Database.GetDbConnection();
                if (Connection.State != System.Data.ConnectionState.Open) await Connection.OpenAsync(ct);
 
                using var Transaction = await Connection.BeginTransactionAsync(ct);
                try
                {
                    // [1순위: Identity First] 시청자 관계 및 닉네임, 마지막 활동 시간 등록
                    const string relationUpsertSql = @"
                        INSERT INTO CoreViewerRelations (StreamerProfileId, GlobalViewerId, IsActive, IsDeleted, AttendanceCount, ConsecutiveAttendanceCount, FirstVisitAt, LastChatAt, CreatedAt, UpdatedAt)
                        VALUES (@StreamerId, @GlobalId, 1, 0, 0, 0, NOW(), NOW(), NOW(), NOW())
                        ON DUPLICATE KEY UPDATE 
                            LastChatAt = NOW(),
                            UpdatedAt = NOW();";

                    await Connection.ExecuteAsync(relationUpsertSql, new { StreamerId = Streamer.Id, GlobalId = GlobalViewerId }, Transaction);
 
                    // [2순위: 포인트/재화 정산]
                    int TotalIncrement = request.AccumulateTotal && request.Amount > 0 ? request.Amount : 0;
 
                    const string UpsertSql = @"
                        INSERT INTO FuncViewerDonations (StreamerProfileId, GlobalViewerId, Balance, TotalDonated, CreatedAt, UpdatedAt)
                        VALUES (@StreamerId, @GlobalId, @Amount, @TotalIncrement, NOW(), NOW())
                        ON DUPLICATE KEY UPDATE 
                            Balance = Balance + @Amount,
                            TotalDonated = TotalDonated + @TotalIncrement,
                            UpdatedAt = NOW();";
 
                    await Connection.ExecuteAsync(UpsertSql, new 
                    { 
                        StreamerId = Streamer.Id, 
                        GlobalId = GlobalViewerId, 
                        Amount = request.Amount,
                        TotalIncrement = TotalIncrement
                    }, Transaction);
 
                    // 2. 최종 잔액 조회 (스냅샷 생성용)
                    var CurrentBalance = await Connection.QueryFirstOrDefaultAsync<int>(
                        "SELECT Balance FROM FuncViewerDonations WHERE StreamerProfileId = @StreamerId AND GlobalViewerId = @GlobalId",
                        new { StreamerId = Streamer.Id, GlobalId = GlobalViewerId }, Transaction);

                    // 3. 감사 로그(FuncViewerDonationHistories) 기록
                    const string LogSql = @"
                        INSERT INTO FuncViewerDonationHistories (StreamerProfileId, GlobalViewerId, PlatformTransactionId, Amount, BalanceAfter, TransactionType, Metadata, CreatedAt, UpdatedAt)
                        VALUES (@StreamerId, @GlobalId, @TxId, @Amount, @BalanceAfter, @Type, @Metadata, NOW(), NOW());";;
 
                    await Connection.ExecuteAsync(LogSql, new
                    {
                        StreamerId = Streamer.Id,
                        GlobalId = GlobalViewerId,
                        TxId = request.PlatformTransactionId ?? $"ADJ-{Guid.NewGuid():N}", // [v7.1] 전송된 SafeEventId 사용
                        Amount = request.Amount,
                        BalanceAfter = CurrentBalance,
                        Type = request.Amount >= 0 ? "DONATION" : "DEDUCTION",
                        Metadata = JsonSerializer.Serialize(new { 
                            Source = request.PlatformTransactionId != null ? "chzzk_gateway" : "manual",
                            RequestAmount = request.Amount 
                        }),
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
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, $"❌ [AddPointsHandler] {request.ViewerUid} 정산 중 오작동: {Ex.Message}");
            return (false, 0);
        }
    }
}
