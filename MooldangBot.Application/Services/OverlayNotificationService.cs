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
            if (string.IsNullOrEmpty(chzzkUid)) return; // [v3.0.0] Clients.All ?�체 브로?�캐?�트 금�? (?�능 보호)
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
            // [?�버?�이??메아�?: ?�측 ?�이??senderId, emojis, payAmount)�??�함??100% ?�합??DTO ?�성
            var chatDto = new ChatOverlayDto(senderId, nickname, userRole, message, emojis, payAmount);
            
            // [?�이???�송 규격]: ?�버?�이??JSON.parse() ?�구?�항??맞춰 문자?�로 직렬??
            var jsonRaw = JsonSerializer.Serialize(chatDto, ChzzkJsonContext.Default.ChatOverlayDto);
            
            // [?�이???�장검�?: 추출?�기 ?�하�?가공된 JSON ?�태�??�세 로그 출력
            if (payAmount > 0)
                logger.LogInformation("?�� [?�버?�이 ?�원 ?�신] Amount: {Amount}, User: {Nickname}", payAmount, nickname);
            else
                logger.LogDebug("?�� [?�버?�이 채팅 ?�신] User: {Nickname}", nickname);
            
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveChat", jsonRaw, token);
        }
    }
}
