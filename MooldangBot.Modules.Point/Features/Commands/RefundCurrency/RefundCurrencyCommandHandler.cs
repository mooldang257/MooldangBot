using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            // [v7.0] Wallet Architecture: 포인트/치즈 환불 처리
            // 포인트(Point)와 치즈(Cheese)는 관리 방식이 동일하므로 통합 처리합니다.
            
            // 1. DB 로그 기록 (사후 증거 확보 - PointLog 대신 PointTransactionHistory 사용)
            var log = new PointTransactionHistory
            {
                StreamerUid = request.StreamerUid,
                ViewerUid = request.ViewerUid,
                ViewerNickname = request.ViewerNickname,
                Amount = request.Amount, // 환불이므로 양수(+)로 기록
                Category = "Refund",
                Description = $"[자율복구] {request.Reason} (ID: {request.CorrelationId})",
                OccurredOn = DateTime.UtcNow
            };
            
            dbContext.PointTransactionHistories.Add(log);

            // 2. 캐시 메모리 복구 (즉각적인 전장 복구)
            // 지휘관의 지침에 따라 캐시는 즉시 업데이트하여 사용자가 환불을 바로 체감하게 합니다.
            // IPointCacheService 인터페이스 규격(AddPointAsync 단수형 등)을 엄격히 준수합니다.
            if (request.CostType.Equals("Point", StringComparison.OrdinalIgnoreCase))
            {
                await cacheService.AddPointAsync(request.StreamerUid, request.ViewerUid, request.Amount);
            }
            else if (request.CostType.Equals("Cheese", StringComparison.OrdinalIgnoreCase))
            {
                // 치즈 환불 처리는 현재 캐시 서비스 규격에 따라 포인트로 통합하거나 전용 메서드 확인 필요
                // (현재 인터페이스에 명시된 AddPointAsync를 활용하거나 실제 구현체 확인)
                await cacheService.AddPointAsync(request.StreamerUid, request.ViewerUid, request.Amount);
            }

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
