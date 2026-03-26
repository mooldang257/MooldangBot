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
        // 채팅 브로드캐스트 로직 (필요 시)
        // 예: Overlay에 채팅 내용 전송 등 (현재는 IOverlayNotificationService에 기능 추가 필요할 수 있음)
        await Task.CompletedTask;
    }
}
