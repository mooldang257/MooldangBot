using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Contracts.Common.Services;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Application.Common.Interfaces;

namespace MooldangBot.Presentation.Features.Debug;

/// <summary>
/// [심연의 현관]: 함선의 가상 장애(Abyssal Trials)를 유발하고 제어하는 테스트용 컨트롤러입니다.
/// </summary>
[ApiController]
[Route("api/chaos")]
public class ChaosController(ChaosManager chaosManager, IChzzkChatService chatService) : ControllerBase
{
    /// <summary>
    /// [v18.0] 가상 Redis 장애(Panic)를 5분간 활성화합니다.
    /// </summary>
    public IActionResult TriggerRedisPanic([FromQuery] int minutes = 5)
    {
        chaosManager.TriggerRedisPanic(TimeSpan.FromMinutes(minutes));
        return Ok(Result<object>.Success(new { Message = $"🔥 [심연의 시련] 가상 Redis 장애가 {minutes}분간 활성화되었습니다." }));
    }

    /// <summary>
    /// [v18.0] 가상 API 지연(Delay)을 5분간 활성화합니다.
    /// </summary>
    public IActionResult TriggerApiDelay([FromQuery] int minutes = 5)
    {
        chaosManager.TriggerApiDelay(TimeSpan.FromMinutes(minutes));
        return Ok(Result<object>.Success(new { Message = $"🌪️ [심연의 시련] 가상 API 지연이 {minutes}분간 활성화되었습니다." }));
    }

    /// <summary>
    /// [v18.0] 모든 가상 장애 상태를 즉시 해제합니다.
    /// </summary>
    public IActionResult Reset()
    {
        chaosManager.Reset();
        return Ok(Result<object>.Success(new { Message = "✅ [심연의 시련] 모든 장애 상황이 종료되었으며, 평화가 찾아왔습니다." }));
    }

    /// <summary>
    /// [v18.0] 특정 채널에 '심연의 시련' 시작을 정식으로 공지합니다.
    /// </summary>
    [HttpPost("notify-trial/{chzzkUid}")]
    public async Task<IActionResult> NotifyTrial(string chzzkUid)
    {
        const string trialMessage = "📢 [오시리스 함선 공지] 현재 '심연의 시련(Abyssal Trials v2.1)'이 시작되었습니다. 함선은 인위적 장애 상황에서의 자가 치유 능력을 검증 중이며, 모든 기능은 폴백 모드로 안전하게 가동 중입니다. ⚓✨";
        
        await chatService.SendMessageAsync(chzzkUid, trialMessage, "SYSTEM_CHAOS");
        
        return Ok(Result<object>.Success(new { Message = "⚓ [심연의 시련] 채널에 정식 공지를 타전했습니다.", Channel = chzzkUid }));
    }
}
