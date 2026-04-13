using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;

namespace MooldangBot.ChzzkAPI.Apis.Chat;

/// <summary>
/// [?ㅼ떆由ъ뒪???꾨졊 - 梨꾪똿]: 移섏?吏?梨꾪똿 硫붿떆吏 諛쒖넚 諛?愿由щ? ?대떦?섎뒗 而⑦듃濡ㅻ윭?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/chat")]
public class ChatController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChzzkApiClient apiClient, ILogger<ChatController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [硫붿떆吏 諛쒖넚]: ?뱀젙 梨꾨꼸??梨꾪똿 硫붿떆吏瑜??꾩넚?⑸땲??
    /// </summary>
    [HttpPost("{chzzkUid}/send")]
    public async Task<IActionResult> SendMessage(string chzzkUid, [FromBody] SendChatRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var result = await _apiClient.SendChatMessageAsync(chzzkUid, request.Message, accessToken);
        if (result == null) return BadRequest("硫붿떆吏 ?꾩넚???ㅽ뙣?섏??듬땲??");

        return Ok(result);
    }

    /// <summary>
    /// [怨듭? ?ㅼ젙]: 梨꾪똿諛??곷떒 怨듭?瑜??깅줉?섍굅???댁젣?⑸땲??
    /// </summary>
    [HttpPost("{chzzkUid}/notice")]
    public async Task<IActionResult> SetNotice(string chzzkUid, [FromBody] SetChatNoticeRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var success = await _apiClient.SetChatNoticeAsync(chzzkUid, request, accessToken);
        return Ok(new { success });
    }

    /// <summary>
    /// [硫붿떆吏 釉붾씪?몃뱶]: ?뱀젙 硫붿떆吏瑜?蹂댁씠吏 ?딄쾶 泥섎━?⑸땲??
    /// </summary>
    [HttpPost("{chzzkUid}/blind")]
    public async Task<IActionResult> BlindMessage(string chzzkUid, [FromBody] BlindMessageRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var success = await _apiClient.BlindMessageAsync(chzzkUid, request, accessToken);
        return Ok(new { success });
    }

    /// <summary>
    /// [梨꾪똿諛??ㅼ젙 議고쉶]: ?꾩옱 梨꾪똿諛⑹쓽 ?꾪꽣留?諛??낆옣 ?쒗븳 ?ㅼ젙??議고쉶?⑸땲??
    /// </summary>
    [HttpGet("{chzzkUid}/settings")]
    public async Task<IActionResult> GetSettings(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var settings = await _apiClient.GetChatSettingsAsync(chzzkUid, accessToken);
        if (settings == null) return NotFound();

        return Ok(settings);
    }
}
