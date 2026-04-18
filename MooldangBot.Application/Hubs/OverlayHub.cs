using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Services;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using MooldangBot.Modules.Roulette.Features.Commands.CompleteRoulette;

namespace MooldangBot.Application.Hubs;

/// <summary>
/// [?�시리스??지?�소]: ?�버?� ?�버?�이 간의 ?�시�?공명 ?�로?�니??
/// (Aegis of Resonance): ?�제 JWT(?�버?�이)?� Cookie(?�?�보?? ?�증??모두 지?�합?�다.
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
[EnableRateLimiting("overlay-high")]
public class OverlayHub(
    IMediator mediator,
    PulseService pulseService,
    ILogger<OverlayHub> logger, 
    IOverlayState overlayState) : Hub
{
    /// <summary>
    /// [v1.9.9] ?�버?�이 ?�니메이???�료 ???�버??결과�??�립?�다.
    /// [Pure Vertical Slice]: 메디?�이?��? ?�해 모듈???�들?�에 ?�임?�니??
    /// </summary>
    public async Task CompleteRouletteAsync(string spinId)
    {
        await mediator.Send(new CompleteRouletteCommand(spinId));
    }

    // [v2.1.0] OBS 브라?��? ?�스 ?�라?�언?��? ?�결?????�출 (JWT ?�레???�용)
    public override async Task OnConnectedAsync()
    {
        // ?�� [?�시리스???��? ?�장]: ?�직 JWT ?�큰 ?�에 ?�명??StreamerId ?�레?�만 ?�뢰?�니??
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;

        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            var normalizedUid = chzzkUid.ToLower();
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedUid);
            await overlayState.IncrementAsync(normalizedUid); // [v13.0] Redis 분산 카운??증�?
            logger.LogInformation("[?�시리스??공명] ?�버?�이 ?�결 ?�공. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", normalizedUid, Context.ConnectionId);
        }
        else
        {
            logger.LogWarning("[?�시리스??불협?�음] ?�효???�레???�는 ?�버?�이 ?�결 ?�도 차단. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            await overlayState.DecrementAsync(chzzkUid.ToLower()); // [v13.0] Redis 분산 카운??감소
        }

        logger.LogTrace("[?�시리스???�상] ?�버?�이 ?�결 종료. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", chzzkUid ?? "Unknown", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// [v2.2.0] ?�라?�언??매개변???�존?�을 ?�거?�고 ?�직 ?�큰???�레?�만 ?�뢰?�니??
    /// </summary>
    public async Task JoinStreamerGroup()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, streamerUid.ToLower());
        }
    }

    public async Task LeaveStreamerGroup()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, streamerUid.ToLower());
        }
    }

    // ?�� ?�?�보???�태 ?�데?�트�??�일 그룹(?�트리머)???�버?�이?�에 ?�송
    public async Task UpdateOverlayState(string stateJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayState", stateJson);
        }
    }

    // ?�정 ?�리??그룹??가??(?�리?�별 ?�립 ?�데?�트 지??
    public async Task JoinPresetGroup(int presetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    public async Task LeavePresetGroup(int presetId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    // ?�리???�이?�웃 ?�데?�트 브로?�캐?�트
    public async Task UpdatePresetStyle(int presetId, string styleJson)
    {
        await Clients.Group($"preset-{presetId}").SendAsync("ReceiveOverlayStyle", styleJson);
    }

    // ?�� ?�자???�정 ?�데?�트�??�일 그룹(?�트리머)???�버?�이?�에 ?�송
    public async Task UpdateOverlayStyle(string styleJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayStyle", styleJson);
        }
    }

    /// <summary>
    /// [v2.2.1] ?�버?�이 ?�라?�언?�의 ?�시�??�존 맥박???�신?�니??
    /// </summary>
    public async Task ReportPulse()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            pulseService.ReportPulse($"Overlay:{streamerUid.ToLower()}");
        }
    }
}
