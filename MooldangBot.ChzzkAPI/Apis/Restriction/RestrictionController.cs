using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Restrictions;

namespace MooldangBot.ChzzkAPI.Apis.Restriction;

/// <summary>
/// [오시리스???ы뙋]: 치지직梨꾨꼸 ?좎? ?쒖옱 諛??쒕룞 ?쒗븳??담당하는 컨트롤러?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/restriction")]
public class RestrictionController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<RestrictionController> _logger;

    public RestrictionController(IChzzkApiClient apiClient, ILogger<RestrictionController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [?쒕룞 ?쒗븳]: 특정 ?좎??먭쾶 ?곴뎄 ?먮뒗 ?쒖떆???쒕룞 ?쒗븳??遺?ы빀?덈떎.
    /// </summary>
    [HttpPost("{chzzkUid}/restrict")]
    public async Task<IActionResult> RestrictUser(string chzzkUid, [FromBody] ChannelRestrictionRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        // [v3.3] IChzzkApiClient??援ы쁽???쒖옱 濡쒖쭅???몄텧?⑸땲??
        // 현재??IChzzkApiClient???대떦 硫붿꽌???ㅽ꽣釉뚭? 援ы쁽?섏뼱 ?덉뼱???⑸땲??
        return Ok(new { message = "?쒖옱 ?붿껌???묒닔?섏뿀입니다 (API 援ы쁽 ?湲?以?" });
    }
}
