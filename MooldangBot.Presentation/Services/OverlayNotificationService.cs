using Microsoft.AspNetCore.SignalR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Presentation.Hubs;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Presentation.Services
{
    public class OverlayNotificationService(
        IHubContext<OverlayHub> hubContext,
        ILogger<OverlayNotificationService> logger) : IOverlayNotificationService
    {
        public async Task NotifyRefreshAsync(string? chzzkUid, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(chzzkUid)) return; // [v3.0.0] Clients.All 전체 브로드캐스트 금지 (성능 보호)
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("SongAdded", "System", "New song request received", token);
        }

        public async Task NotifyRouletteResultAsync(string chzzkUid, SpinRouletteResponse response, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveRouletteResult", response, token);
        }

        public async Task NotifyMissionReceivedAsync(string chzzkUid, RouletteLog missionLog, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("MissionReceived", missionLog, token);
        }

        public async Task NotifySongQueueChangedAsync(string chzzkUid, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RefreshSongAndDashboard", cancellationToken: token);
        }

        public async Task NotifyChatReceivedAsync(string chzzkUid, string nickname, string message, string userRole, CancellationToken token = default)
        {
            // [오버레이의 메아리]: 익명 객체 대신 명시적 DTO(ChatOverlayMessage)를 사용하여 직렬화 안정성 확보
            var chatMessage = new ChatOverlayMessage(nickname, message, userRole, System.DateTime.UtcNow);
            
            // [데이터 현장검증]: 추출하기 편하게 JSON 형태로 상세 로그 출력
            var jsonLog = JsonSerializer.Serialize(chatMessage, new JsonSerializerOptions { WriteIndented = true });
            logger.LogInformation("📤 [오버레이 송신 데이터 포맷]\n{Json}", jsonLog);
            
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveChatMessage", chatMessage, token);
        }
    }
}
