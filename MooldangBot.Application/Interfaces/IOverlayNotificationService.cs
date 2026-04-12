using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Interfaces;

public interface IOverlayNotificationService
{
    Task NotifyRefreshAsync(string? chzzkUid, CancellationToken token = default);
    Task NotifyRouletteResultAsync(string chzzkUid, SpinRouletteResponse response, CancellationToken token = default);
    Task NotifyMissionReceivedAsync(string chzzkUid, RouletteLog missionLog, CancellationToken token = default);
    Task NotifySongQueueChangedAsync(string chzzkUid, CancellationToken token = default);
    Task NotifyPointChangedAsync(string chzzkUid, CancellationToken token = default);
    Task NotifyChatReceivedAsync(string chzzkUid, string senderId, string nickname, string message, string userRole, System.Text.Json.JsonElement? emojis = null, int? payAmount = null, CancellationToken token = default);
}
