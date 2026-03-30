using Microsoft.AspNetCore.SignalR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Presentation.Hubs;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Presentation.Services
{
    public class OverlayNotificationService(IHubContext<OverlayHub> hubContext) : IOverlayNotificationService
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
            // [오버레이의 메아리]: 실시간 채팅 데이터를 해당 채널 오버레이 그룹에만 전송
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveChatMessage", new {
                nickname,
                message,
                userRole,
                timestamp = System.DateTime.UtcNow
            }, token);
        }
    }
}
