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
/// [?¤ì‹œë¦¬ìŠ¤??ì§€?˜ì†Œ]: ?œë²„?€ ?¤ë²„?ˆì´ ê°„ì˜ ?¤ì‹œê°?ê³µëª… ?µë¡œ?…ë‹ˆ??
/// (Aegis of Resonance): ?´ì œ JWT(?¤ë²„?ˆì´)?€ Cookie(?€?œë³´?? ?¸ì¦??ëª¨ë‘ ì§€?í•©?ˆë‹¤.
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
    /// [v1.9.9] ?¤ë²„?ˆì´ ? ë‹ˆë©”ì´???„ë£Œ ???œë²„??ê²°ê³¼ë¥??Œë¦½?ˆë‹¤.
    /// [Pure Vertical Slice]: ë©”ë””?ì´?°ë? ?µí•´ ëª¨ë“ˆ???¸ë“¤?¬ì— ?„ì„?©ë‹ˆ??
    /// </summary>
    public async Task CompleteRouletteAsync(string spinId)
    {
        await mediator.Send(new CompleteRouletteCommand(spinId));
    }

    // [v2.1.0] OBS ë¸Œë¼?°ì? ?ŒìŠ¤ ?´ë¼?´ì–¸?¸ê? ?°ê²°?????¸ì¶œ (JWT ?´ë ˆ???„ìš©)
    public override async Task OnConnectedAsync()
    {
        // ?” [?¤ì‹œë¦¬ìŠ¤???ˆë? ?¸ì¥]: ?¤ì§ JWT ? í° ?´ì— ?œëª…??StreamerId ?´ë ˆ?„ë§Œ ? ë¢°?©ë‹ˆ??
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;

        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            var normalizedUid = chzzkUid.ToLower();
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedUid);
            await overlayState.IncrementAsync(normalizedUid); // [v13.0] Redis ë¶„ì‚° ì¹´ìš´??ì¦ê?
            logger.LogInformation("[?¤ì‹œë¦¬ìŠ¤??ê³µëª…] ?¤ë²„?ˆì´ ?°ê²° ?±ê³µ. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", normalizedUid, Context.ConnectionId);
        }
        else
        {
            logger.LogWarning("[?¤ì‹œë¦¬ìŠ¤??ë¶ˆí˜‘?”ìŒ] ? íš¨???´ë ˆ???†ëŠ” ?¤ë²„?ˆì´ ?°ê²° ?œë„ ì°¨ë‹¨. ConnectionId: {ConnectionId}", Context.ConnectionId);
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
            await overlayState.DecrementAsync(chzzkUid.ToLower()); // [v13.0] Redis ë¶„ì‚° ì¹´ìš´??ê°ì†Œ
        }

        logger.LogTrace("[?¤ì‹œë¦¬ìŠ¤???”ìƒ] ?¤ë²„?ˆì´ ?°ê²° ì¢…ë£Œ. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", chzzkUid ?? "Unknown", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// [v2.2.0] ?´ë¼?´ì–¸??ë§¤ê°œë³€???˜ì¡´?±ì„ ?œê±°?˜ê³  ?¤ì§ ? í°???´ë ˆ?„ë§Œ ? ë¢°?©ë‹ˆ??
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

    // ?’¡ ?€?œë³´???íƒœ ?…ë°?´íŠ¸ë¥??™ì¼ ê·¸ë£¹(?¤íŠ¸ë¦¬ë¨¸)???¤ë²„?ˆì´?¤ì— ?„ì†¡
    public async Task UpdateOverlayState(string stateJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayState", stateJson);
        }
    }

    // ?¹ì • ?„ë¦¬??ê·¸ë£¹??ê°€??(?„ë¦¬?‹ë³„ ?…ë¦½ ?…ë°?´íŠ¸ ì§€??
    public async Task JoinPresetGroup(int presetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    public async Task LeavePresetGroup(int presetId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    // ?„ë¦¬???ˆì´?„ì›ƒ ?…ë°?´íŠ¸ ë¸Œë¡œ?œìº?¤íŠ¸
    public async Task UpdatePresetStyle(int presetId, string styleJson)
    {
        await Clients.Group($"preset-{presetId}").SendAsync("ReceiveOverlayStyle", styleJson);
    }

    // ?’¡ ?”ì???¤ì • ?…ë°?´íŠ¸ë¥??™ì¼ ê·¸ë£¹(?¤íŠ¸ë¦¬ë¨¸)???¤ë²„?ˆì´?¤ì— ?„ì†¡
    public async Task UpdateOverlayStyle(string styleJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayStyle", styleJson);
        }
    }

    /// <summary>
    /// [v2.2.1] ?¤ë²„?ˆì´ ?´ë¼?´ì–¸?¸ì˜ ?¤ì‹œê°??ì¡´ ë§¥ë°•???˜ì‹ ?©ë‹ˆ??
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
