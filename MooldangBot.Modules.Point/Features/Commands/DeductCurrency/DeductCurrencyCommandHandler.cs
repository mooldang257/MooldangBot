using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Point.Enums;
using MooldangBot.Contracts.Point.Requests.Commands;
using MooldangBot.Contracts.Security;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Domain.Entities;
using System.Data;

namespace MooldangBot.Modules.Point.Features.Commands.DeductCurrency;

/// <summary>
/// [v7.0] 통합 재화 차감 핸들러: Dapper를 사용하여 DB 레벨에서 원자적 감산을 수행합니다.
/// 사용자의 '선결제 후실행' 파이프라인의 핵심 결제 엔진입니다.
/// </summary>
public class DeductCurrencyCommandHandler : IRequestHandler<DeductCurrencyCommand, DeductResult>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IPointDbContext _db; // Streamer/Viewer ID 조회를 위한 하이브리드 접근
    private readonly IPointCacheService _pointCache; // ChatPoint의 경우 Redis 증분값 포함 검증 필요

    public DeductCurrencyCommandHandler(
        IDbConnectionFactory dbConnectionFactory,
        IPointDbContext db,
        IPointCacheService pointCache)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _db = db;
        _pointCache = pointCache;
    }

    public async Task<DeductResult> Handle(DeductCurrencyCommand request, CancellationToken ct)
    {
        // 1. [하이브리드 조회]: 닉네임/ID 등을 기반으로 내부 정수형 ID 확보
        var viewerHash = Sha256Hasher.ComputeHash(request.ViewerUid);
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
            return new DeductResult(false, 0, "시청자 지갑 정보가 존재하지 않습니다.");

        // 2. [ChatPoint 특수 처리]: Redis 증분값을 포함한 잔액 검증
        if (request.CurrencyType == PointCurrencyType.ChatPoint)
        {
            var dbBalance = await _db.ViewerPoints.AsNoTracking()
                .Where(v => v.StreamerProfileId == streamer.Id && v.GlobalViewerId == globalViewerId)
                .Select(v => v.Points)
                .FirstOrDefaultAsync(ct);

            var redisIncrement = await _pointCache.GetIncrementalPointAsync(request.StreamerUid, request.ViewerUid);
            var totalBalance = dbBalance + redisIncrement;

            if (totalBalance < request.Amount)
            {
                return new DeductResult(false, totalBalance, "포인트가 부족합니다.");
            }

            // 고빈도 포인트는 캐시에 음수 증분으로 기록 (Write-Back 동기화 대기)
            await _pointCache.AddPointAsync(request.StreamerUid, request.ViewerUid, -request.Amount);
            return new DeductResult(true, totalBalance - request.Amount);
        }

        // 3. [유료 재화(DonationPoint) 원자적 처리]: Dapper를 통한 DB 직접 차감
        using var conn = _dbConnectionFactory.CreateConnection();
        
        // 🛡️ [철옹성 쿼리]: 잔액이 차감액보다 클 때만 업데이트하고, 영향받은 행 수를 통해 성공 여부 판단
        const string updateSql = @"
            UPDATE viewer_donations 
            SET balance = balance - @Amount, updated_at = NOW()
            WHERE streamer_profile_id = @StreamerId 
              AND global_viewer_id = @GlobalId 
              AND balance >= @Amount"; // 마이너스 방지

        var affectedRows = await conn.ExecuteAsync(updateSql, new 
        { 
            StreamerId = streamer.Id, 
            GlobalId = globalViewerId, 
            Amount = request.Amount 
        });

        if (affectedRows == 0)
        {
            // 차감 실패 시 현재 잔액 조회하여 반환
            var currentBalance = await conn.QueryFirstOrDefaultAsync<int>(
                "SELECT balance FROM viewer_donations WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
                new { StreamerId = streamer.Id, GlobalId = globalViewerId });

            return new DeductResult(false, currentBalance, "치즈 잔액이 부족합니다.");
        }

        // 성공 시 최종 잔액 조회
        var finalBalance = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT balance FROM viewer_donations WHERE streamer_profile_id = @StreamerId AND global_viewer_id = @GlobalId",
            new { StreamerId = streamer.Id, GlobalId = globalViewerId });

        return new DeductResult(true, finalBalance);
    }
}
