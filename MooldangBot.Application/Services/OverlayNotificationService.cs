using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MooldangBot.Contracts.Chzzk;
using MooldangBot.Contracts.Models.Chzzk;
using MooldangBot.Application.Hubs;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Services
{
    public class OverlayNotificationService(
        IHubContext<OverlayHub> hubContext,
        ILogger<OverlayNotificationService> logger) : IOverlayNotificationService
    {
        public async Task NotifyRefreshAsync(string? chzzkUid, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(chzzkUid)) return; // [v3.0.0] Clients.All ?ÑÏ≤¥ Î∏åÎ°ú?úÏ∫ê?§Ìä∏ Í∏àÏ? (?±Îä• Î≥¥Ìò∏)
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
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("NotifySongQueueChanged", cancellationToken: token);
        }

        public async Task NotifyPointChangedAsync(string chzzkUid, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RefreshSongAndDashboard", cancellationToken: token);
        }

        public async Task NotifyChatReceivedAsync(string chzzkUid, string senderId, string nickname, string message, string userRole, System.Text.Json.JsonElement? emojis = null, int? payAmount = null, CancellationToken token = default)
        {
            // [?§Î≤Ñ?àÏù¥??Î©îÏïÑÎ¶?: ?§Ï∏° ?∞Ïù¥??senderId, emojis, payAmount)Î•??¨Ìï®??100% ?ïÌï©??DTO ?ùÏÑ±
            var chatDto = new ChatOverlayDto(senderId, nickname, userRole, message, emojis, payAmount);
            
            // [?∞Ïù¥???ÑÏÜ° Í∑úÍ≤©]: ?§Î≤Ñ?àÏù¥??JSON.parse() ?îÍµ¨?¨Ìï≠??ÎßûÏ∂∞ Î¨∏Ïûê?¥Î°ú ÏßÅÎ†¨??
            var jsonRaw = JsonSerializer.Serialize(chatDto, ChzzkJsonContext.Default.ChatOverlayDto);
            
            // [?∞Ïù¥???ÑÏû•Í≤ÄÏ¶?: Ï∂îÏ∂ú?òÍ∏∞ ?∏ÌïòÍ≤?Í∞ÄÍ≥µÎêú JSON ?ïÌÉúÎ°??ÅÏÑ∏ Î°úÍ∑∏ Ï∂úÎ†•
            if (payAmount > 0)
                logger.LogInformation("?í∞ [?§Î≤Ñ?àÏù¥ ?ÑÏõê ?°Ïã†] Amount: {Amount}, User: {Nickname}", payAmount, nickname);
            else
                logger.LogDebug("?ì§ [?§Î≤Ñ?àÏù¥ Ï±ÑÌåÖ ?°Ïã†] User: {Nickname}", nickname);
            
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveChat", jsonRaw, token);
        }
    }
}
