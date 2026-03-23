using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Features.Chat.Events;
using MooldangAPI.Hubs;
using MooldangAPI.Models;

namespace MooldangAPI.Features.SongQueue.Handlers
{
    public class SongRequestEventHandler : INotificationHandler<ChatMessageReceivedEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SongRequestEventHandler> _logger;

        public SongRequestEventHandler(IServiceProvider serviceProvider, ILogger<SongRequestEventHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
        {
            string msg = notification.Message.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            // !신청 또는 !곡신청 명령어 확인
            if (msg.StartsWith("!신청") || msg.StartsWith("!곡신청"))
            {
                string songTitle = "";
                if (msg.StartsWith("!신청 ")) songTitle = msg.Substring(4).Trim();
                else if (msg.StartsWith("!곡신청 ")) songTitle = msg.Substring(5).Trim();
                else return; // 곡 제목이 없는 경우

                if (string.IsNullOrEmpty(songTitle)) return;

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<OverlayHub>>();

                var chzzkUid = notification.Profile.ChzzkUid;

                // 1. 현재 활성화된 세션이 있는지 확인 (선택 사항)
                var activeSession = await db.SonglistSessions
                    .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid && s.IsActive, cancellationToken);

                if (activeSession == null)
                {
                    _logger.LogWarning($"⚠️ [곡 신청 무시] 스트리머 {chzzkUid}의 송리스트 세션이 비활성화 상태입니다.");
                    return;
                }

                // 2. DB에 곡 추가
                var newSong = new MooldangAPI.Models.SongQueue
                {
                    ChzzkUid = chzzkUid,
                    Title = songTitle,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    SortOrder = await db.SongQueues.Where(s => s.ChzzkUid == chzzkUid).CountAsync(cancellationToken)
                };

                db.SongQueues.Add(newSong);
                activeSession.RequestCount++;
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"🎵 [곡 신청 수락] {notification.Username} -> {songTitle}");

                // 3. 오버레이 갱신 신호 발송 (소문자 UID 그룹 사용)
                string groupName = chzzkUid.ToLower();
                await hubContext.Clients.Group(groupName).SendAsync("RefreshSongAndDashboard", cancellationToken: cancellationToken);
                await hubContext.Clients.Group(groupName).SendAsync("SongAdded", notification.Username, songTitle, cancellationToken: cancellationToken);
            }
        }
    }
}
