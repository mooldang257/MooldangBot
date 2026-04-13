using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Shared;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Internal;

namespace MooldangBot.ChzzkAPI.Apis.Internal;

/// <summary>
/// [오시리스의 감시]: 게이트웨이 내 샤드들의 실시간 연결 상태 및 부하 정도를 노출합니다.
/// </summary>
[ApiController]
[Route("api/internal/shards")]
public class InternalShardController : ControllerBase
{
    private readonly IShardedWebSocketManager _shardManager;
    private readonly ILogger<InternalShardController> _logger;
    private const string InternalSecretHeader = "X-Internal-Secret-Key";

    public InternalShardController(IShardedWebSocketManager shardManager, ILogger<InternalShardController> logger)
    {
        _shardManager = shardManager;
        _logger = logger;
    }

    /// <summary>
    /// [오시리스의 지표]: 현재 가동 중인 모든 샤드의 상태 리스트를 반환합니다.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatuses()
    {
        if (!IsAuthorized()) return Unauthorized();

        _logger.LogInformation("📡 [Gateway] 샤드 상태 정보 요청 수신.");
        
        var statuses = await _shardManager.GetShardStatusesAsync();
        
        return Ok(new ChzzkApiResponse<IEnumerable<ShardStatus>>
        {
            Code = 200,
            Content = statuses
        });
    }

    private bool IsAuthorized()
    {
        // [이지스의 방패]: 내부 통신 시 시크릿 키 검증
        if (!Request.Headers.TryGetValue(InternalSecretHeader, out var secret) || 
            secret != Environment.GetEnvironmentVariable("INTERNAL_API_SECRET"))
        {
            _logger.LogWarning("⚠️ [Gateway] 권한 없는 샤드 상태 조회 시도가 차단되었습니다.");
            return false;
        }
        return true;
    }
}
