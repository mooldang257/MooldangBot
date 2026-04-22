using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Point.Interfaces;
using MooldangBot.Modules.Point.Commands; // [v7.4] 성지로 이전된 규격 참조
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Modules.Point.Features.Commands.RefundCurrency;

/// <summary>
/// [오시리스의 치유사]: RefundCurrencyCommand를 수신하여 재화를 안전하게 복구합니다.
/// </summary>
public class RefundCurrencyCommandHandler(
    IPointDbContext dbContext,
    IPointCacheService cacheService,
    IIdentityCacheService identityCache,
    ILogger<RefundCurrencyCommandHandler> logger) : IRequestHandler<RefundCurrencyCommand, bool>
{
    public async Task<bool> Handle(RefundCurrencyCommand request, CancellationToken ct)
    {
        try
        {
            // 0. 기반 정보 조회 (오시리스의 탐색): Uid를 통해 실제 ProfileId와 GlobalViewerId를 확보합니다.
            var streamerProfile = await dbContext.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid == request.StreamerUid, ct);
            
            // [이지스 통합]: UID 기반 시청자 식별 및 동기화
            var globalViewerId = await identityCache.SyncGlobalViewerIdAsync(request.ViewerUid, request.ViewerNickname ?? "Unknown", null, ct);

            if (streamerProfile == null || globalViewerId == 0)
            {
                logger.LogWarning("⚠️ [자율 복구 실패] 대상 프로필 또는 시청자를 식별할 수 없습니다. (Uid: {Uid})", request.ViewerUid);
                return false;
            }

            // [v7.0] Wallet Architecture: 포인트/치즈 환불 처리
            
            // 1. DB 로그 기록 (천상의 장부 기입)
            // PointTransactionHistory의 실제 규격(StreamerProfileId, GlobalViewerId, KstClock)을 준수합니다.
            var log = new PointTransactionHistory
            {
                StreamerProfileId = streamerProfile.Id,
                GlobalViewerId = globalViewerId,
                Amount = request.Amount,
                Type = PointTransactionType.System, // [v7.3] 오시리스 규률: 환불은 시스템 보정 트랜잭션으로 취급합니다.
                Reason = $"[자율복구] {request.Reason} (ID: {request.CorrelationId})",
                CreatedAt = KstClock.Now
            };
            
            dbContext.PointTransactionHistories.Add(log);

            // 2. 캐시 메모리 복구
            // IPointCacheService 인터페이스 규격(AddPointAsync)을 사용합니다.
            await cacheService.AddPointAsync(request.StreamerUid, request.ViewerUid, request.ViewerNickname ?? "Unknown", request.Amount);

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
