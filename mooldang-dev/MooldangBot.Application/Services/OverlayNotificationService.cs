using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.DTOs;
using MooldangBot.Application.Hubs;
using MooldangBot.Domain.Contracts.Hubs;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Contracts.Chzzk;

using MooldangBot.Domain.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Services
{
    public class OverlayNotificationService(
        IHubContext<OverlayHub, IOverlayClient> hubContext,
        IAppDbContext db,
        ILogger<OverlayNotificationService> logger) : IOverlayNotificationService
    {
        public async Task NotifyRefreshAsync(string? chzzkUid, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(chzzkUid)) return; 
            await hubContext.Clients.Group(chzzkUid.ToLower()).SongAdded("System", "New song request received");
        }

        public async Task NotifyRouletteResultAsync(string chzzkUid, SpinRouletteResponse response, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).ReceiveRouletteResult(response);
        }

        public async Task NotifyMissionReceivedAsync(string chzzkUid, RouletteMissionOverlayDto missionDto, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).MissionReceived(missionDto);
        }

        public async Task NotifySongQueueChangedAsync(string chzzkUid, CancellationToken token = default)
        {
            // [물멍]: 단순히 신호만 보내는 구식 방식입니다. 가급적 BroadcastSongOverlayUpdateAsync를 사용하세요.
            await hubContext.Clients.Group(chzzkUid.ToLower()).NotifySongQueueChanged();
        }

        public async Task NotifyPointChangedAsync(string chzzkUid, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).RefreshSongAndDashboard();
        }

        public async Task NotifyChatReceivedAsync(string chzzkUid, string senderId, string nickname, string message, string userRole, JsonElement? emojis = null, int? payAmount = null, CancellationToken token = default)
        {
            var chatDto = new ChatOverlayDto(senderId, nickname, userRole, message, emojis, payAmount);
            var jsonRaw = JsonSerializer.Serialize(chatDto, ChzzkJsonContext.Default.ChatOverlayDto);
            
            await hubContext.Clients.Group(chzzkUid.ToLower()).ReceiveChat(jsonRaw);
        }

        public async Task NotifySongOverlayUpdateAsync(string chzzkUid, SongOverlayDto data, CancellationToken token = default)
        {
            await hubContext.Clients.Group(chzzkUid.ToLower()).ReceiveSongOverlayUpdate(data);
        }

        public async Task BroadcastSongOverlayUpdateAsync(string chzzkUid, string? connectionId = null, CancellationToken token = default)
        {
            var normalizedUid = chzzkUid.ToLower();
            
            // 1. 스트리머 프로필 및 설정 조회
            var profile = await db.CoreStreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == normalizedUid, token);

            if (profile == null) return;

            // 2. 현재 재생 중인 곡 조회
            var currentSong = await db.FuncSongQueues
                .AsNoTracking()
                .Where(s => s.StreamerProfileId == profile.Id && s.Status == SongStatus.Playing && !s.IsDeleted)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync(token);

            // 3. 설정 파싱 (디자인 설정) - MaxQueueCount 등을 위해 대기열 조회 전에 수행
            var settings = new SongOverlaySettings();
            if (!string.IsNullOrEmpty(profile.DesignSettingsJson))
            {
                try {
                    var parsed = JsonSerializer.Deserialize(profile.DesignSettingsJson, ChzzkJsonContext.Default.SongOverlaySettings);
                    if (parsed != null) settings = parsed;
                } catch { /* 기본값 유지 */ }
            }

            // 4. 대기열 곡 조회 (인라인 테마는 개수 제한 없음)
            var query = db.FuncSongQueues
                .AsNoTracking()
                .Include(s => s.GlobalViewer)
                .Where(s => s.StreamerProfileId == profile.Id && s.Status == SongStatus.Pending && !s.IsDeleted)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Id);

            List<SongQueue> queueSongs;
            if (settings.QueueTheme == "inline")
            {
                queueSongs = await query.ToListAsync(token);
            }
            else
            {
                int queueCount = settings.MaxQueueCount > 0 ? settings.MaxQueueCount : 5;
                queueSongs = await query.Take(queueCount).ToListAsync(token);
            }

            // 5. DTO 조립
            var dto = new SongOverlayDto(
                currentSong != null ? new CurrentSongDto(currentSong.Id, currentSong.Title, currentSong.Artist, currentSong.VideoId, currentSong.ThumbnailUrl) : null,
                queueSongs.Select(s => new QueueSongDto(
                    s.Id,
                    s.Title, 
                    s.Artist, 
                    s.RequesterNickname ?? s.GlobalViewer?.Nickname ?? "익명",
                    s.VideoId,
                    s.ThumbnailUrl)).ToList(),
                settings
            );

            // 6. 전송 (특정 연결 대상 또는 그룹 전체)
            if (!string.IsNullOrEmpty(connectionId))
            {
                await hubContext.Clients.Client(connectionId).ReceiveSongOverlayUpdate(dto);
            }
            else
            {
                await hubContext.Clients.Group(normalizedUid).ReceiveSongOverlayUpdate(dto);
            }

            logger.LogInformation("[오시리스의 공명] 신청곡 오버레이 상태 브로드캐스트 완료. Channel: {ChzzkUid}, State: {State}", normalizedUid, currentSong != null ? "Playing" : "Idle");
        }
    }
}
