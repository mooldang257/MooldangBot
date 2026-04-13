using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Restrictions;

namespace MooldangBot.ChzzkAPI.Apis.Restriction;

/// <summary>
/// [?ㅼ떆由ъ뒪???ы뙋]: 移섏?吏?梨꾨꼸 ?좎? ?쒖옱 諛??쒕룞 ?쒗븳???대떦?섎뒗 而⑦듃濡ㅻ윭?낅땲??
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
    /// [?쒕룞 ?쒗븳]: ?뱀젙 ?좎??먭쾶 ?곴뎄 ?먮뒗 ?쒖떆???쒕룞 ?쒗븳??遺?ы빀?덈떎.
    /// </summary>
    [HttpPost("{chzzkUid}/restrict")]
    public async Task<IActionResult> RestrictUser(string chzzkUid, [FromBody] ChannelRestrictionRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        // [v3.3] IChzzkApiClient??援ы쁽???쒖옱 濡쒖쭅???몄텧?⑸땲??
        // ?꾩옱??IChzzkApiClient???대떦 硫붿꽌???ㅽ꽣釉뚭? 援ы쁽?섏뼱 ?덉뼱???⑸땲??
        return Ok(new { message = "?쒖옱 ?붿껌???묒닔?섏뿀?듬땲?? (API 援ы쁽 ?湲?以?" });
    }
}
