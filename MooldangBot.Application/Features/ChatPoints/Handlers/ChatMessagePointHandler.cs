using MooldangBot.Contracts.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MediatR;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Contracts.Chzzk.Models.Events;

namespace MooldangBot.Application.Features.ChatPoints.Handlers;

/// <summary>
/// [경제 수호자]: 모든 채팅 메시지를 수신하여 시청자에게 포인트를 지급하는 핸들러입니다.
/// (P2: 공정성): 5초 쿨다운 정책을 적용하여 도배로 인한 어뷰징을 사전에 차단합니다.
/// </summary>
public class ChatMessagePointHandler(
    IPointBatchService batchService,
    ILogger<ChatMessagePointHandler> logger) : INotificationHandler<ChzzkEventReceived>
{

    public Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        // 1. [다형성 선별]: 채팅 이벤트만 처리
        if (notification.Payload is not ChzzkChatEvent chat)
            return Task.CompletedTask;

        // 2. [시스템 배제]: 발신자 정보가 없는 경우 제외 (봇은 이미 중앙에서 거름)
        if (string.IsNullOrEmpty(chat.SenderId))
            return Task.CompletedTask;

        // 3. [공명 전파]: 모든 채팅 메시지에 대해 즉시 포인트 적립 요청을 배치 서비스로 위임
        batchService.EnqueueIncrement(
            notification.Profile.ChzzkUid!, 
            chat.SenderId, 
            chat.Nickname, 
            1 // 기본 채팅당 1점 적립
        );

        logger.LogDebug("✨ [포인트 적립 완료] {Username}: +1", chat.Nickname);

        return Task.CompletedTask;
    }
}
