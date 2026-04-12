using Microsoft.AspNetCore.Mvc;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Session;

namespace MooldangBot.ChzzkAPI.Apis.Session;

/// <summary>
/// [?ㅼ떆由ъ뒪????뎄 - ?몄뀡]: 移섏?吏?梨꾪똿 ?몄뀡 ?곌껐 諛??대깽??援щ룆???대떦?섎뒗 而⑦듃濡ㅻ윭?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/session")]
public class SessionController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<SessionController> _logger;

    public SessionController(IChzzkApiClient apiClient, ILogger<SessionController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [?몄뀡 URL ?띾뱷]: ?뱀젙 梨꾨꼸??梨꾪똿 ?뱀냼耳??묒냽???꾪븳 ?몄뀡 URL??媛?몄샃?덈떎.
    /// </summary>
    [HttpGet("{chzzkUid}/url")]
    public async Task<IActionResult> GetSessionUrl(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var url = await _apiClient.GetSessionUrlAsync(chzzkUid, accessToken);
        if (url == null) return NotFound("?몄뀡 URL??媛?몄삤吏 紐삵뻽?듬땲??");

        return Ok(url);
    }

    /// <summary>
    /// [?대깽??援щ룆]: ?뱀젙 ?몄뀡?먯꽌 諛쒖깮?섎뒗 梨꾪똿 ?대깽?몃? ?쒕쾭(WebsocketShard)??釉뚮줈?쒖틦?ㅽ듃?섎룄濡??붿껌?⑸땲??
    /// </summary>
    [HttpPost("{chzzkUid}/subscribe")]
    public async Task<IActionResult> SubscribeSession(string chzzkUid, [FromBody] SubscribeEventRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var success = await _apiClient.SubscribeSessionEventAsync(chzzkUid, request.SessionKey, "chat", accessToken);
        return Ok(new { success });
    }
}
