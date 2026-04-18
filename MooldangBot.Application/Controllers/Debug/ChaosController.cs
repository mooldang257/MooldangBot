using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Contracts.Common.Services;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Application.Common.Interfaces;

namespace MooldangBot.Application.Controllers.Debug;

/// <summary>
/// [?¬ì—°???„ê?]: ?¨ì„ ??ê°€???¥ì• (Abyssal Trials)ë¥?? ë°œ?˜ê³  ?œì–´?˜ëŠ” ?ŒìŠ¤?¸ìš© ì»¨íŠ¸ë¡¤ëŸ¬?…ë‹ˆ??
/// </summary>
[ApiController]
[Route("api/chaos")]
public class ChaosController(ChaosManager chaosManager, IChzzkChatService chatService) : ControllerBase
{
    /// <summary>
    /// [v18.0] ê°€??Redis ?¥ì• (Panic)ë¥?5ë¶„ê°„ ?œì„±?”í•©?ˆë‹¤.
    /// </summary>
    public IActionResult TriggerRedisPanic([FromQuery] int minutes = 5)
    {
        chaosManager.TriggerRedisPanic(TimeSpan.FromMinutes(minutes));
        return Ok(Result<object>.Success(new { Message = $"?”¥ [?¬ì—°???œë ¨] ê°€??Redis ?¥ì• ê°€ {minutes}ë¶„ê°„ ?œì„±?”ë˜?ˆìŠµ?ˆë‹¤." }));
    }

    /// <summary>
    /// [v18.0] ê°€??API ì§€??Delay)??5ë¶„ê°„ ?œì„±?”í•©?ˆë‹¤.
    /// </summary>
    public IActionResult TriggerApiDelay([FromQuery] int minutes = 5)
    {
        chaosManager.TriggerApiDelay(TimeSpan.FromMinutes(minutes));
        return Ok(Result<object>.Success(new { Message = $"?Œªï¸?[?¬ì—°???œë ¨] ê°€??API ì§€?°ì´ {minutes}ë¶„ê°„ ?œì„±?”ë˜?ˆìŠµ?ˆë‹¤." }));
    }

    /// <summary>
    /// [v18.0] ëª¨ë“  ê°€???¥ì•  ?íƒœë¥?ì¦‰ì‹œ ?´ì œ?©ë‹ˆ??
    /// </summary>
    public IActionResult Reset()
    {
        chaosManager.Reset();
        return Ok(Result<object>.Success(new { Message = "??[?¬ì—°???œë ¨] ëª¨ë“  ?¥ì•  ?í™©??ì¢…ë£Œ?˜ì—ˆ?¼ë©°, ?‰í™”ê°€ ì°¾ì•„?”ìŠµ?ˆë‹¤." }));
    }

    /// <summary>
    /// [v18.0] ?¹ì • ì±„ë„??'?¬ì—°???œë ¨' ?œì‘???•ì‹?¼ë¡œ ê³µì??©ë‹ˆ??
    /// </summary>
    [HttpPost("notify-trial/{chzzkUid}")]
    public async Task<IActionResult> NotifyTrial(string chzzkUid)
    {
        const string trialMessage = "?“¢ [?¤ì‹œë¦¬ìŠ¤ ?¨ì„  ê³µì?] ?„ì¬ '?¬ì—°???œë ¨(Abyssal Trials v2.1)'???œì‘?˜ì—ˆ?µë‹ˆ?? ?¨ì„ ?€ ?¸ìœ„???¥ì•  ?í™©?ì„œ???ê? ì¹˜ìœ  ?¥ë ¥??ê²€ì¦?ì¤‘ì´ë©? ëª¨ë“  ê¸°ëŠ¥?€ ?´ë°± ëª¨ë“œë¡??ˆì „?˜ê²Œ ê°€??ì¤‘ì…?ˆë‹¤. ?“âœ¨";
        
        await chatService.SendMessageAsync(chzzkUid, trialMessage, "SYSTEM_CHAOS");
        
        return Ok(Result<object>.Success(new { Message = "??[?¬ì—°???œë ¨] ì±„ë„???•ì‹ ê³µì?ë¥??€?„í–ˆ?µë‹ˆ??", Channel = chzzkUid }));
    }
}
