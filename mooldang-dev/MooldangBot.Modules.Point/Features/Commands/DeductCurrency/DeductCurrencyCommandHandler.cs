using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Point.Enums;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Point.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using System.Data;

namespace MooldangBot.Modules.Point.Features.Commands.DeductCurrency;

/// <summary>
/// [v7.0] 통합 재화 차감 핸들러: Dapper를 사용하여 DB 레벨에서 원자적 감산을 수행합니다.
/// [v7.1] 후원 적립 로직 통합: 후원 시점에 따라 누적액 반영 여부를 결정합니다.
/// </summary>
public class DeductCurrencyCommandHandler : IRequestHandler<DeductCurrencyCommand, DeductResult>
{
    private readonly IPointDbContext _db;
    private readonly IPointCacheService _pointCache;
    private readonly IMediator _mediator; // [물멍]: 후원금 적립을 위해 추가

    public DeductCurrencyCommandHandler(
        IPointDbContext db,
        IPointCacheService pointCache,
        IMediator mediator)
    {
        _db = db;
        _pointCache = pointCache;
        _mediator = mediator;
    }

    public async Task<DeductResult> Handle(DeductCurrencyCommand request, CancellationToken ct)
    {
        // 1. [하이브리드 조회]: 닉네임/ID 등을 기반으로 내부 정수형 ID 확보
        var cleanedUid = (request.ViewerUid ?? "").Trim();
        var viewerHash = Sha256Hasher.ComputeHash(cleanedUid);
        
        var streamer = await _db.StreamerProfiles.AsNoTracking()
            .Select(s => new { s.Id, s.ChzzkUid })
            .FirstOrDefaultAsync(s => s.ChzzkUid == request.StreamerUid, ct);
        
        if (streamer == null) 
            return new DeductResult(false, 0, "스트리머 정보를 찾을 수 없습니다.");

        var globalViewerId = await _db.GlobalViewers.AsNoTracking()
            .Where(g => g.ViewerUidHash == viewerHash)
            .Select(g => g.Id)
            .FirstOrDefaultAsync(ct);

        if (globalViewerId == 0)
        {
            try
            {
                // [오시리스의 자비]: 지갑이 없는 신규 유저라면 즉석에서 개설합니다. (JIT Onboarding)
                var newViewer = new GlobalViewer 
                { 
                    ViewerUid = cleanedUid, 
                    ViewerUidHash = viewerHash, 
                    Nickname = request.ViewerNickname ?? "시청자" 
                };
                _db.GlobalViewers.Add(newViewer);
                await _db.SaveChangesAsync(ct);
                globalViewerId = newViewer.Id;
            }
            catch (DbUpdateException)
            {
                // 🛡️ [동시성 방어]: 찰나의 순간에 다른 쓰레드가 생성했다면, 조용히 다시 조회합니다.
                globalViewerId = await _db.GlobalViewers.AsNoTracking()
                    .Where(g => g.ViewerUidHash == viewerHash)
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync(ct);
                
                if (globalViewerId == 0) throw; // 진짜 알 수 없는 오류라면 상위로 전파
            }
        }

        // 2. [ChatPoint 특수 처리]: Redis 증분값을 포함한 잔액 검증
        if (request.CurrencyType == PointCurrencyType.ChatPoint)
        {
            var dbBalance = await _db.ViewerPoints.AsNoTracking()
                .Where(v => v.StreamerProfileId == streamer.Id && v.GlobalViewerId == globalViewerId)
                .Select(v => v.Points)
                .FirstOrDefaultAsync(ct);

            var redisIncrement = await _pointCache.GetIncrementalPointAsync(request.StreamerUid, cleanedUid);
            var totalBalance = dbBalance + redisIncrement;

            if (totalBalance < request.Amount)
            {
                return new DeductResult(false, totalBalance, "포인트가 부족합니다.");
            }

            // 고빈도 포인트는 캐시에 음수 증분으로 기록 (Write-Back 동기화 대기)
            await _pointCache.AddPointAsync(request.StreamerUid, cleanedUid, request.ViewerNickname ?? "Unknown", -request.Amount);
            return new DeductResult(true, totalBalance - request.Amount);
        }

        // 3. [유료 재화(DonationPoint) 원자적 처리]
        // [물멍]: 먼저 후원금을 지갑에 적립합니다. (후원 적립 여부 반영)
        if (request.DonationAmount > 0)
        {
            await _mediator.Send(new AddPointsCommand(
                request.StreamerUid,
                cleanedUid,
                request.ViewerNickname ?? "시청자",
                request.DonationAmount,
                PointCurrencyType.DonationPoint,
                null,
                request.AccumulateTotal), ct);
        }

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);
        
        // 🛡️ [철옹성 쿼리]: 현장 결제 시스템(Real-time Donation Credit)
        // [복구]: 사용자께서 구현하셨던 '후원금을 가용 금액으로 설정'하는 로직을 통합 구조에 맞게 적용합니다.
        // 현재 DB 잔액이 부족하더라도, 이번 요청에서 함께 들어온 후원금(@DonationAmount)이 있다면 결제를 허용합니다.
        const string updateSql = @"
            UPDATE viewer_donations 
            SET balance = balance - @DeductAmount, updated_at = NOW()
            WHERE streamer_profile_id = @StreamerId 
              AND global_viewer_id = @GlobalId 
              AND (balance + @DonationAmount >= @DeductAmount)";

        var affectedRows = await conn.ExecuteAsync(updateSql, new 
        { 
            StreamerId = streamer.Id, 
            GlobalId = globalViewerId, 
            DeductAmount = request.Amount,
            DonationAmount = request.DonationAmount
        });

        if (affectedRows == 0)
        {
            var currentBalance = await conn.QueryFirstOrDefaultAsync<int>(
                "SELECT balance FROM viewer_donations WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalViewerId });

            return new DeductResult(false, currentBalance, "치즈 잔액이 부족합니다.");
        }

        var finalBalance = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT balance FROM viewer_donations WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
            new { StreamerId = streamer.Id, GlobalId = globalViewerId });

        return new DeductResult(true, finalBalance);
    }
}
