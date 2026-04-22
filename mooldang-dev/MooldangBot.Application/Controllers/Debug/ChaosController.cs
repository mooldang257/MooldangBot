using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Common.Services;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Application.Common.Interfaces;

namespace MooldangBot.Application.Controllers.Debug;

/// <summary>
/// [?�연???��?]: ?�선??가???�애(Abyssal Trials)�??�발?�고 ?�어?�는 ?�스?�용 컨트롤러?�니??
/// </summary>
[ApiController]
[Route("api/chaos")]
public class ChaosController(ChaosManager chaosManager, IChzzkChatService chatService) : ControllerBase
{
    /// <summary>
    /// [v18.0] 가??Redis ?�애(Panic)�?5분간 ?�성?�합?�다.
    /// </summary>
    public IActionResult TriggerRedisPanic([FromQuery] int minutes = 5)
    {
        chaosManager.TriggerRedisPanic(TimeSpan.FromMinutes(minutes));
        return Ok(Result<object>.Success(new { Message = $"?�� [?�연???�련] 가??Redis ?�애가 {minutes}분간 ?�성?�되?�습?�다." }));
    }

    /// <summary>
    /// [v18.0] 가??API 지??Delay)??5분간 ?�성?�합?�다.
    /// </summary>
    public IActionResult TriggerApiDelay([FromQuery] int minutes = 5)
    {
        chaosManager.TriggerApiDelay(TimeSpan.FromMinutes(minutes));
        return Ok(Result<object>.Success(new { Message = $"?���?[?�연???�련] 가??API 지?�이 {minutes}분간 ?�성?�되?�습?�다." }));
    }

    /// <summary>
    /// [v18.0] 모든 가???�애 ?�태�?즉시 ?�제?�니??
    /// </summary>
    public IActionResult Reset()
    {
        chaosManager.Reset();
        return Ok(Result<object>.Success(new { Message = "??[?�연???�련] 모든 ?�애 ?�황??종료?�었?�며, ?�화가 찾아?�습?�다." }));
    }

    /// <summary>
    /// [v18.0] ?�정 채널??'?�연???�련' ?�작???�식?�로 공�??�니??
    /// </summary>
    [HttpPost("notify-trial/{chzzkUid}")]
    public async Task<IActionResult> NotifyTrial(string chzzkUid)
    {
        const string trialMessage = "?�� [?�시리스 ?�선 공�?] ?�재 '?�연???�련(Abyssal Trials v2.1)'???�작?�었?�니?? ?�선?� ?�위???�애 ?�황?�서???��? 치유 ?�력??검�?중이�? 모든 기능?� ?�백 모드�??�전?�게 가??중입?�다. ?�✨";
        
        await chatService.SendMessageAsync(chzzkUid, trialMessage, "SYSTEM_CHAOS");
        
        return Ok(Result<object>.Success(new { Message = "??[?�연???�련] 채널???�식 공�?�??�?�했?�니??", Channel = chzzkUid }));
    }
}
