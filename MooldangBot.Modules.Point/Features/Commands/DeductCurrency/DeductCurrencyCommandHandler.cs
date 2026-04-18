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
/// 사용자의 '선결제 후실행' 파이프라인의 핵심 결제 엔진입니다.
/// </summary>
public class DeductCurrencyCommandHandler : IRequestHandler<DeductCurrencyCommand, DeductResult>
{
    private readonly IPointDbContext _db; // Streamer/Viewer ID 조회를 위한 하이브리드 접근
    private readonly IPointCacheService _pointCache; // ChatPoint의 경우 Redis 증분값 포함 검증 필요

    public DeductCurrencyCommandHandler(
        IPointDbContext db,
        IPointCacheService pointCache)
    {
        _db = db;
        _pointCache = pointCache;
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
            await _pointCache.AddPointAsync(request.StreamerUid, cleanedUid, -request.Amount);
            return new DeductResult(true, totalBalance - request.Amount);
        }

        // 3. [유료 재화(DonationPoint) 원자적 처리]: Dapper를 통한 DB 직접 차감
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);
        
        // ⚔️ [선제 타격]: 지갑 레코드가 아예 없는 경우 0원 잔액 행을 생성하여 차감 쿼리가 작동하도록 함
        const string ensureWalletSql = @"
            INSERT IGNORE INTO viewer_donations (streamer_profile_id, global_viewer_id, balance, total_donated, created_at, updated_at)
            VALUES (@StreamerId, @GlobalId, 0, 0, NOW(), NOW());";
        
        await conn.ExecuteAsync(ensureWalletSql, new { StreamerId = streamer.Id, GlobalId = globalViewerId });

        // 🛡️ [철옹성 쿼리]: 현장 결제 시스템(Real-time Donation Credit) 적용
        // 지금 막 후원한 금액(DonationAmount)을 결제액에서 선제적으로 제하고, 남은 금액만 DB 잔액에서 깎습니다.
        var actualDeductAmount = Math.Max(0, request.Amount - request.DonationAmount);

        const string updateSql = @"
            UPDATE viewer_donations 
            SET balance = balance - @DeductAmount, updated_at = NOW()
            WHERE streamer_profile_id = @StreamerId 
              AND global_viewer_id = @GlobalId 
              AND balance >= @DeductAmount"; // 마이너스 방지

        var affectedRows = await conn.ExecuteAsync(updateSql, new 
        { 
            StreamerId = streamer.Id, 
            GlobalId = globalViewerId, 
            DeductAmount = actualDeductAmount 
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
