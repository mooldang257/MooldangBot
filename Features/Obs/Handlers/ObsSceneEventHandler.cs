using MediatR;
using MooldangAPI.Features.Chat.Events;
using MooldangAPI.Services;
using Microsoft.Extensions.Logging;

namespace MooldangAPI.Features.Obs.Handlers
{
    public class ObsSceneEventHandler : INotificationHandler<ChatMessageReceivedEvent>
    {
        private readonly ObsWebSocketService _obsService;
        private readonly ILogger<ObsSceneEventHandler> _logger;

        public ObsSceneEventHandler(ObsWebSocketService obsService, ILogger<ObsSceneEventHandler> logger)
        {
            _obsService = obsService;
            _logger = logger;
        }

        public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
        {
            var msg = notification.Message.Trim();

            // !장면 [장면이름] 또는 !화면 [장면이름]
            if (msg.StartsWith("!장면 ") || msg.StartsWith("!화면 "))
            {
                // 스트리머 또는 매니저만 권한 부여 (권한 체크)
                if (notification.UserRole == "streamer" || notification.UserRole == "manager" || notification.SenderId == "ca98875d5e0edf02776047fbc70f5449")
                {
                    string sceneName = msg.Substring(4).Trim();
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        _logger.LogInformation($"OBS Scene switch requested: {sceneName} by {notification.Nickname}");
                        
                        // 실제 운영 시에는 스트리머 프로필에 저장된 OBS 설정으로 Connect 후 전송해야 하지만, 
                        // 여기서는 기본 구조만 구현합니다.
                        _obsService.SetScene(sceneName);
                    }
                }
            }
        }
    }
}
