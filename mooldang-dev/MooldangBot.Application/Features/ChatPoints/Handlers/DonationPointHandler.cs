using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using MediatR;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using MooldangBot.Modules.Point.Interfaces;

namespace MooldangBot.Application.Features.ChatPoints.Handlers;

/// <summary>
/// [후원의 은총]: 후원 발생 시 설정된 비율에 따라 시청자에게 포인트를 지급하는 핸들러입니다.
/// </summary>
public class DonationPointHandler(
    IPointCacheService pointCache,
    ILogger<DonationPointHandler> logger) : INotificationHandler<ChzzkEventReceived>
{
    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        // 1. [유형 선별]: 후원 이벤트만 처리
        if (notification.Payload is not ChzzkDonationEvent Donation)
            return;
 
        // 2. [조건 확인]: 포인트 설정값이 있는지 확인 (UI 옵션과 관계없이 포인트가 설정되어 있으면 지급)
        if (notification.Profile.PointPerDonation1000 <= 0)
            return;
 
        // 3. [포인트 계산]: 1,000원당 설정된 포인트만큼 지급
        // 예: 5,000원 후원, 1000원당 10포인트 설정 -> 50포인트
        int PointsToGive = (int)Math.Floor((Donation.PayAmount / 1000.0) * notification.Profile.PointPerDonation1000);
 
        if (PointsToGive <= 0)
            return;
 
        // 4. [적립 실행]: Redis 캐시로 즉시 적재
        try
        {
            logger.LogInformation("💰 [후원 포인트] {Nickname}님 {Amount}원 후원 -> {Points}포인트 적립 시도", 
                Donation.Nickname, Donation.PayAmount, PointsToGive);
 
            await pointCache.AddPointAsync(
                notification.Profile.ChzzkUid, 
                Donation.SenderId, 
                Donation.Nickname, 
                PointsToGive
            );
        }
        catch (Exception Ex)
        {
            logger.LogError(Ex, "❌ [후원 포인트 적립 실패] {Nickname}: {Msg}", Donation.Nickname, Ex.Message);
        }
    }
}
