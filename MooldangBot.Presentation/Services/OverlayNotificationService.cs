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
            if (string.IsNullOrEmpty(chzzkUid))
            {
                await hubContext.Clients.All.SendAsync("SongAdded", "System", "New song request received", token);
            }
            else
            {
                await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("SongAdded", "System", "New song request received", token);
            }
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
    }
}
