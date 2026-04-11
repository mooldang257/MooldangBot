using Microsoft.AspNetCore.Mvc;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Drops;

namespace MooldangBot.ChzzkAPI.Apis.Drops;

/// <summary>
/// [?ㅼ떆由ъ뒪???섏궗??: 移섏?吏??쒕∼??由ъ썙??愿由щ? ?대떦?섎뒗 而⑦듃濡ㅻ윭?낅땲??
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
    /// [由ъ썙??紐⑸줉 議고쉶]: ?꾩옱 ?ъ슜?먭? ?띾뱷 媛?ν븳 ?쒕∼??由ъ썙??紐⑸줉??議고쉶?⑸땲??
    /// </summary>
    [HttpGet("claims")]
    public async Task<IActionResult> GetClaims([FromHeader(Name = "Authorization")] string authHeader)
    {
        // [v3.3] IChzzkApiClient ?ъ뼇??留욎떠 援ы쁽 ?湲?以?(?명꽣?섏씠???뺥빀???뺣낫 ?곗꽑)
        return Ok(new { claims = new object[] { } });
    }
}
