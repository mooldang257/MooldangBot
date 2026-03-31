using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Chat.Handlers;

public class ChatBroadcastEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly ILogger<ChatBroadcastEventHandler> _logger;
    private readonly IOverlayNotificationService _overlayService;

    public ChatBroadcastEventHandler(ILogger<ChatBroadcastEventHandler> logger, IOverlayNotificationService overlayService)
    {
        _logger = logger;
        _overlayService = overlayService;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        // [오버레이의 메아리]: 수신된 채팅을 해당 스트리머의 오버레이 그룹으로 즉시 전송합니다.
        if (notification.Profile != null && !string.IsNullOrEmpty(notification.Profile.ChzzkUid))
        {
            await _overlayService.NotifyChatReceivedAsync(
                notification.Profile.ChzzkUid,
                notification.SenderId,
                notification.Username,
                notification.Message,
                notification.UserRole,
                notification.Emojis,
                cancellationToken);
        }
    }
}
