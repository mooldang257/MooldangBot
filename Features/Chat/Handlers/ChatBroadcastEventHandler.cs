using MediatR;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Data;
using MooldangAPI.Features.Chat.Events;
using MooldangAPI.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MooldangAPI.Features.Chat.Handlers;

public class ChatBroadcastEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IHubContext<OverlayHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatBroadcastEventHandler> _logger;

    public ChatBroadcastEventHandler(IHubContext<OverlayHub> hubContext, IServiceProvider serviceProvider, ILogger<ChatBroadcastEventHandler> logger)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Handle(ChatMessageReceivedEvent req, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var chzzkUid = req.Profile.ChzzkUid;

        // Avatar 설정 확인 (활성화 된 경우에만 처리할지 결정)
        var avatarSetting = await db.AvatarSettings.FirstOrDefaultAsync(x => x.ChzzkUid == chzzkUid, cancellationToken);
        bool isAvatarEnabled = avatarSetting?.IsEnabled ?? true;

        // "채팅"만 별도로 방송할 수도 있고, 아바타용으로만 보낼 수도 있습니다.
        // 우리는 "ReceiveChat"이라는 공통 이벤트를 오버레이 쪽으로 보냅니다.
        var chatMsg = new
        {
            nickname = req.Username,
            message = req.Message,
            userRole = req.UserRole, // "common_user", "subscriber", "tier2", "streamer", "manager" 등
            senderId = req.SenderId,
            emojis = req.Emojis
        };
        
        string msgText = req.Message.Trim();

        if (string.IsNullOrEmpty(chzzkUid)) return;

        // 일반 채팅 방송 (모든 오버레이용)
        // [수정] isAvatarEnabled 여부와 상관없이 ReceiveChat을 전송하여 룰렛 등 다른 오버레이도 채팅을 인식하도록 함
        await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveChat", JsonSerializer.Serialize(chatMsg), cancellationToken);

        // 아바타 애니메이션 명령 처리
        if (msgText == "!달리기" || msgText == "!비행")
        {
            var commandMsg = new
            {
                nickname = req.Username,
                command = msgText == "!달리기" ? "run" : "fly",
                userRole = req.UserRole,
                senderId = req.SenderId
            };
            
            await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveAvatarCommand", JsonSerializer.Serialize(commandMsg), cancellationToken);
            _logger.LogInformation($"[아바타] {req.Username} 님이 {msgText} 애니메이션을 실행했습니다.");
        }
    }
}
