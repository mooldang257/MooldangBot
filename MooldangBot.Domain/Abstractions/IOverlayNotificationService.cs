using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Domain.Abstractions;

public interface IOverlayNotificationService
{
    Task NotifyRefreshAsync(string? chzzkUid, CancellationToken token = default);
    Task NotifyRouletteResultAsync(string chzzkUid, SpinRouletteResponse response, CancellationToken token = default);
    Task NotifyMissionReceivedAsync(string chzzkUid, RouletteMissionOverlayDto missionDto, CancellationToken token = default);
    Task NotifySongQueueChangedAsync(string chzzkUid, CancellationToken token = default);
    Task NotifyPointChangedAsync(string chzzkUid, CancellationToken token = default);
    Task NotifyChatReceivedAsync(string chzzkUid, string senderId, string nickname, string message, string userRole, System.Text.Json.JsonElement? emojis = null, int? payAmount = null, CancellationToken token = default);
    
    // [v16.0] 신청곡 오버레이 전용 실시간 상태 동기화
    Task NotifySongOverlayUpdateAsync(string chzzkUid, SongOverlayDto data, CancellationToken token = default);

    /// <summary>
    /// [물멍]: 현재 상태(Playing, Pending, Settings)를 DB에서 직접 조회하여 오버레이에 브로드캐스트합니다.
    /// </summary>
    Task BroadcastSongOverlayUpdateAsync(string chzzkUid, string? connectionId = null, CancellationToken token = default);
}
