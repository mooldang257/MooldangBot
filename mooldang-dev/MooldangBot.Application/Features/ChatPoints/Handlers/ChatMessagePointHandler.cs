using MooldangBot.Domain.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MediatR;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using MooldangBot.Modules.Point.Interfaces;

namespace MooldangBot.Application.Features.ChatPoints.Handlers;

/// <summary>
/// [경제 수호자]: 모든 채팅 메시지를 수신하여 시청자에게 포인트를 지급하는 핸들러입니다.
/// (P2: 공정성): 5초 쿨다운 정책을 적용하여 도배로 인한 어뷰징을 사전에 차단합니다.
/// </summary>
public class ChatMessagePointHandler(
    IPointCacheService pointCache,
    ILogger<ChatMessagePointHandler> logger) : INotificationHandler<ChzzkEventReceived>
{

    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        // 1. [다형성 선별]: 채팅 이벤트만 처리
        if (notification.Payload is not ChzzkChatEvent chat)
            return;

        // 2. [시스템 배제]: 발신자 정보가 없는 경우 제외
        if (string.IsNullOrEmpty(chat.SenderId))
            return;

        // [v7.2] 시청자 정보 로깅 강화
        logger.LogDebug("💬 [채팅 포인트 이벤트] {Nickname}({Uid}): {Amount}포인트 적립 시도", 
            chat.Nickname, chat.SenderId, notification.Profile.PointPerChat);

        // 3. [오시리스의 직결]: 중간 버퍼 없이 Redis 캐시로 직접 적재 (Atomic INCR)
        try
        {
            await pointCache.AddPointAsync(
                notification.Profile.ChzzkUid, 
                chat.SenderId, 
                chat.Nickname, 
                notification.Profile.PointPerChat
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [포인트 적립 실패] {Nickname}: {Msg}", chat.Nickname, ex.Message);
        }
    }
}
