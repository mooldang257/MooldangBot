using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Modules.Point.Features.Commands.RefundCurrency;

/// <summary>
/// [오시리스의 자율 복구]: 명령어 실행 실패 시 차감된 재화를 원래대로 되돌리는 환불 명령입니다.
/// </summary>
public record RefundCurrencyCommand(
    string StreamerUid,
    string ViewerUid,
    string ViewerNickname,
    int Amount,
    string CostType,
    string Reason,
    Guid CorrelationId
) : IRequest<bool>;

/// <summary>
/// [오시리스의 치유사]: RefundCurrencyCommand를 수신하여 재화를 안전하게 복구합니다.
/// </summary>
public class RefundCurrencyCommandHandler(
    IPointDbContext dbContext,
    IPointCacheService cacheService,
    ILogger<RefundCurrencyCommandHandler> logger) : IRequestHandler<RefundCurrencyCommand, bool>
{
    public async Task<bool> Handle(RefundCurrencyCommand request, CancellationToken ct)
    {
        try
        {
            // 0. 기반 정보 조회 (오시리스의 탐색): Uid를 통해 실제 ProfileId와 GlobalViewerId를 확보합니다.
            var streamerProfile = await dbContext.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid == request.StreamerUid, ct);
            
            var globalViewer = await dbContext.GlobalViewers
                .FirstOrDefaultAsync(v => v.Nickname == request.ViewerNickname, ct); // 닉네임 또는 UID 해시 기반 조회 필요

            if (streamerProfile == null || globalViewer == null)
            {
                logger.LogWarning("⚠️ [자율 복구 실패] 대상 프로필 또는 시청자를 찾을 수 없습니다. (Viewer: {Viewer})", request.ViewerNickname);
                return false;
            }

            // [v7.0] Wallet Architecture: 포인트/치즈 환불 처리
            
            // 1. DB 로그 기록 (천상의 장부 기입)
            // PointTransactionHistory의 실제 규격(StreamerProfileId, GlobalViewerId, KstClock)을 준수합니다.
            var log = new PointTransactionHistory
            {
                StreamerProfileId = streamerProfile.Id,
                GlobalViewerId = globalViewer.Id,
                Amount = request.Amount,
                Type = PointTransactionType.Refund, // [v7.2] 전용 거래 타입 사용
                Reason = $"[자율복구] {request.Reason} (ID: {request.CorrelationId})",
                CreatedAt = KstClock.Now
            };
            
            dbContext.PointTransactionHistories.Add(log);

            // 2. 캐시 메모리 복구
            // IPointCacheService 인터페이스 규격(AddPointAsync)을 사용합니다.
            await cacheService.AddPointAsync(request.StreamerUid, request.ViewerUid, request.Amount);

            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("💖 [자율 복구 성공] {Viewer}님께 {Amount} {Type} 환불 완료. (사유: {Reason}, CorrId: {Id})", 
                request.ViewerNickname, request.Amount, request.CostType, request.Reason, request.CorrelationId);
                
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [자율 복구 실패] {Viewer}님 환불 중 오류 발생. (CorrId: {Id})", request.ViewerUid, request.CorrelationId);
            return false;
        }
    }
}
