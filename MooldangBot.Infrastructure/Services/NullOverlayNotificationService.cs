using MooldangBot.Domain.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v2.4.8] 백그라운드 워커 전용 더미 오버레이 알림 서비스
/// SignalR 허브가 없는 환경(Bot Engine)에서 IOverlayNotificationService 의존성 해결을 위해 사용됩니다.
/// </summary>
public class NullOverlayNotificationService : IOverlayNotificationService
{
    public Task NotifyRefreshAsync(string? chzzkUid, CancellationToken token = default) => Task.CompletedTask;
    
    public Task NotifyRouletteResultAsync(string chzzkUid, SpinRouletteResponse response, CancellationToken token = default) => Task.CompletedTask;
    
    public Task NotifyMissionReceivedAsync(string chzzkUid, MooldangBot.Domain.DTOs.RouletteMissionOverlayDto missionDto, CancellationToken token = default) => Task.CompletedTask;
    
    public Task NotifySongQueueChangedAsync(string chzzkUid, CancellationToken token = default) => Task.CompletedTask;
    
    public Task NotifyPointChangedAsync(string chzzkUid, CancellationToken token = default) => Task.CompletedTask;
    
    public Task NotifyChatReceivedAsync(string chzzkUid, string senderId, string nickname, string message, string userRole, JsonElement? emojis = null, int? payAmount = null, CancellationToken token = default) => Task.CompletedTask;
}
