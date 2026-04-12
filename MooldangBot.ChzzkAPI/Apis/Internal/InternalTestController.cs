using Microsoft.AspNetCore.Mvc;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Internal;

namespace MooldangBot.ChzzkAPI.Apis.Internal;

/// <summary>
/// [오시리스의 시험장]: 외부 시뮬레이터로부터 치지직 원본 이벤트를 주입받아 전체 파이프라인을 검증합니다.
/// </summary>
[ApiController]
[Route("api/internal/test")]
public class InternalTestController : ControllerBase
{
    private readonly IShardedWebSocketManager _shardManager;
    private readonly ILogger<InternalTestController> _logger;
    private const string InternalSecretHeader = "X-Internal-Secret-Key";

    public InternalTestController(IShardedWebSocketManager shardManager, ILogger<InternalTestController> logger)
    {
        _shardManager = shardManager;
        _logger = logger;
    }

    /// <summary>
    /// [v3.6] 치지직 원본 JSON 이벤트를 특정 채널의 샤드로 주입합니다.
    /// </summary>
    [HttpPost("inject")]
    public async Task<IActionResult> InjectEvent([FromBody] InjectEventRequest request)
    {
        if (!IsAuthorized()) return Unauthorized();

        if (string.IsNullOrEmpty(request.ChzzkUid) || string.IsNullOrEmpty(request.RawJson))
        {
            return BadRequest("ChzzkUid and RawJson are required.");
        }

        var success = await _shardManager.InjectEventAsync(request.ChzzkUid, request.EventName, request.RawJson);
        
        if (success)
        {
            _logger.LogInformation("🧪 [Mock] 채널 {ChzzkUid}에 {EventName} 이벤트 주입 성공", request.ChzzkUid, request.EventName);
            return Ok($"Event {request.EventName} injected to {request.ChzzkUid}");
        }
        else
        {
            _logger.LogWarning("⚠️ [Mock] 채널 {ChzzkUid}에 이벤트 주입 실패 (샤드를 찾을 수 없거나 연결되지 않음)", request.ChzzkUid);
            return NotFound($"Shard or Channel {request.ChzzkUid} not found or not active");
        }
    }

    private bool IsAuthorized()
    {
        if (!Request.Headers.TryGetValue(InternalSecretHeader, out var secret) || 
            secret != Environment.GetEnvironmentVariable("INTERNAL_API_SECRET"))
        {
            _logger.LogWarning("⚠️ [Security] 미인증 테스트 접근 차단");
            return false;
        }
        return true;
    }
}
