using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Drops;

namespace MooldangBot.ChzzkAPI.Apis.Drops;

/// <summary>
/// [오시리스???섏궗??: 치지직?쒕∼??由ъ썙??愿由щ? 담당하는 컨트롤러?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/drops")]
public class DropsController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<DropsController> _logger;

    public DropsController(IChzzkApiClient apiClient, ILogger<DropsController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [由ъ썙??紐⑸줉 조회]: 현재 ?ъ슜?먭? ?띾뱷 媛?ν븳 ?쒕∼??由ъ썙??紐⑸줉??조회?⑸땲??
    /// </summary>
    [HttpGet("claims")]
    public async Task<IActionResult> GetClaims([FromHeader(Name = "Authorization")] string authHeader)
    {
        // [v3.3] IChzzkApiClient ?ъ뼇??留욎떠 援ы쁽 ?湲?以?(?명꽣?섏씠???뺥빀???뺣낫 ?곗꽑)
        return Ok(new { claims = new object[] { } });
    }
}
