using MediatR;
using MooldangAPI.Features.Chat.Events;
using MooldangAPI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Data;
using MooldangAPI.Hubs;

namespace MooldangAPI.Features.Obs.Handlers
{
    public class ObsSceneEventHandler : INotificationHandler<ChatMessageReceivedEvent>
    {
        private readonly ObsWebSocketService _obsService;
        private readonly ILogger<ObsSceneEventHandler> _logger;
        private readonly AppDbContext _db;
        private readonly IHubContext<OverlayHub> _hubContext;
 
        public ObsSceneEventHandler(ObsWebSocketService obsService, ILogger<ObsSceneEventHandler> logger, AppDbContext db, IHubContext<OverlayHub> hubContext)
        {
            _obsService = obsService;
            _logger = logger;
            _db = db;
            _hubContext = hubContext;
        }

        public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
        {
            var msg = notification.Message.Trim();

            if (msg.StartsWith("!장면 ") || msg.StartsWith("!화면 "))
            {
                if (notification.UserRole == "streamer" || notification.UserRole == "manager" || notification.SenderId == "ca98875d5e0edf02776047fbc70f5449")
                {
                    string sceneName = msg.Substring(4).Trim();
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        var chzzkUid = notification.Profile.ChzzkUid;
                        if (string.IsNullOrEmpty(chzzkUid)) return;

                        // 1. 프리셋 검색
                        var preset = await _db.OverlayPresets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid && p.Name == sceneName, cancellationToken);

                        if (preset != null)
                        {
                            _logger.LogInformation($"[Preset Switch] Switching to preset: {sceneName} for {chzzkUid}");

                            // 2. 스트리머 프로필 업데이트
                            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, cancellationToken);
                            if (profile != null)
                            {
                                profile.ActiveOverlayPresetId = preset.Id;
                                await _db.SaveChangesAsync(cancellationToken);
                            }

                            // 3. SignalR 브로드캐스트 (실시간 레이아웃 교체)
                            await _hubContext.Clients.Group(chzzkUid).SendAsync("ReceiveOverlayStyle", preset.ConfigJson, cancellationToken);
                        }
                        else
                        {
                            _logger.LogWarning($"[Preset Switch] Preset not found: {sceneName} for {chzzkUid}");
                        }
                        
                        // 4. (기본) OBS WebSocket 장면 전환 시도
                        _obsService.SetScene(sceneName);
                    }
                }
            }
        }
    }
}
