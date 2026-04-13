using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Contracts.Models.Chzzk;
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
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("NotifySongQueueChanged", cancellationToken: token);
        }

        public async Task NotifyPointChangedAsync(string chzzkUid, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RefreshSongAndDashboard", cancellationToken: token);
        }

        public async Task NotifyChatReceivedAsync(string chzzkUid, string senderId, string nickname, string message, string userRole, System.Text.Json.JsonElement? emojis = null, int? payAmount = null, CancellationToken token = default)
        {
            // [오버레이의 메아리]: 실측 데이터(senderId, emojis, payAmount)를 포함한 100% 정합성 DTO 생성
            var chatDto = new ChatOverlayDto(senderId, nickname, userRole, message, emojis, payAmount);
            
            // [데이터 전송 규격]: 오버레이의 JSON.parse() 요구사항에 맞춰 문자열로 직렬화
            var jsonRaw = JsonSerializer.Serialize(chatDto, ChzzkJsonContext.Default.ChatOverlayDto);
            
            // [데이터 현장검증]: 추출하기 편하게 가공된 JSON 형태로 상세 로그 출력
            if (payAmount > 0)
                logger.LogInformation("💰 [오버레이 후원 송신] Amount: {Amount}, User: {Nickname}", payAmount, nickname);
            else
                logger.LogDebug("📤 [오버레이 채팅 송신] User: {Nickname}", nickname);
            
            await hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveChat", jsonRaw, token);
        }
    }
}
