using Microsoft.AspNetCore.Mvc;
using MooldangBot.Application.Interfaces;
using System.Threading.Tasks;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [공명의 전령]: 오버레이 또는 외부 시스템으로부터의 방송 상태 신호를 처리합니다.
/// </summary>
[ApiController]
[Route("api/stream")]
public class StreamSignalController(
    IBroadcastScribe scribe,
    IChzzkBotService botService) : ControllerBase
{
    /// <summary>
    /// [각성의 신호]: 오버레이 하트비트를 수신하여 세션을 유지하고 봇을 활성화합니다.
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromQuery] string chzzkUid)
    {
        await scribe.HeartbeatAsync(chzzkUid);
        
        // [피닉스의 재건]: 세션이 활성 상태이면 봇 연결 보장
        await botService.EnsureConnectionAsync(chzzkUid);
        
        return Ok();
    }

    /// <summary>
    /// [엔딩 크레딧의 여운]: 스트리머가 수동으로 방송을 종료할 때 호출되어 통계를 산출합니다.
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> StopStream([FromQuery] string chzzkUid)
    {
        var stats = await scribe.FinalizeSessionAsync(chzzkUid);
        if (stats == null) return NotFound("Active session not found.");
        
        return Ok(stats);
    }
}
