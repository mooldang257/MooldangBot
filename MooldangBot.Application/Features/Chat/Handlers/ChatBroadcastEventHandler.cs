using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Events;
using MediatR;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Events;
using MooldangBot.Contracts.Chzzk.Models.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Chat.Handlers;

/// <summary>
/// [오버레이의 전령]: 채팅 및 후원 이벤트를 실시간으로 방송하여 오버레이 화면에 표시하는 핸들러입니다.
/// </summary>
public class ChatBroadcastEventHandler(
    ILogger<ChatBroadcastEventHandler> logger, 
    IOverlayNotificationService overlayService) : INotificationHandler<ChzzkEventReceived>
{
    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        // 1. [다형성 분기]: 이벤트 타입에 따라 적절한 데이터 추출 및 전파
        if (notification.Payload is ChzzkChatEvent chat)
        {
            logger.LogInformation("📢 [{ChannelId}] 채팅 오버레이 송신: {Nickname}", notification.Profile.ChzzkUid, chat.Nickname);
            await overlayService.NotifyChatReceivedAsync(
                notification.Profile.ChzzkUid!,
                chat.SenderId,
                chat.Nickname,
                chat.Content,
                chat.UserRoleCode ?? "common_user",
                chat.Emojis,
                null, // 일반 채팅은 금액 없음
                ct);
        }
        else if (notification.Payload is ChzzkDonationEvent donation)
        {
            logger.LogInformation("💰 [{ChannelId}] 후원 오버레이 송신: {Nickname} ({Amount}치즈)", notification.Profile.ChzzkUid, donation.Nickname, donation.PayAmount);
            await overlayService.NotifyChatReceivedAsync(
                notification.Profile.ChzzkUid!,
                donation.SenderId,
                donation.Nickname,
                donation.DonationMessage,
                "donation_user",
                null,
                donation.PayAmount, // 후원 금액 전달
                ct);
        }
    }
}
