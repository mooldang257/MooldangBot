using Microsoft.AspNetCore.Mvc;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Live;

namespace MooldangBot.ChzzkAPI.Apis.Live;

/// <summary>
/// [?ㅼ떆由ъ뒪??臾대?]: 移섏?吏?諛⑹넚 ?ㅼ젙 諛??ㅽ듃由?愿由щ? ?대떞?섎뒗 而⑦듃濡ㅻ윭?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/live")]
public class LiveController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<LiveController> _logger;

    public LiveController(IChzzkApiClient apiClient, ILogger<LiveController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [諛⑹넚 ?ㅼ젙 議고쉶]: ?꾩옱 梨꾨꼸??諛⑹넚 ?쒕ぉ, 移댄뀒怨좊━ ?깆쓣 議고쉶?⑸땲??
    /// </summary>
    [HttpGet("{chzzkUid}/settings")]
    public async Task<IActionResult> GetSettings(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var settings = await _apiClient.GetLiveSettingAsync(chzzkUid, accessToken);
        if (settings == null) return NotFound("諛⑹넚 ?ㅼ젙??媛?몄삤吏 紐삵뻽?듬땲??");

        return Ok(settings);
    }

    /// <summary>
    /// [諛⑹넚 ?ㅼ젙 ?낅뜲?댄듃]: 諛⑹넚 ?쒕ぉ, 移댄뀒怨좊━ ?깆쓣 蹂寃쏀빀?덈떎.
    /// </summary>
    [HttpPatch("{chzzkUid}/settings")]
    public async Task<IActionResult> UpdateSettings(string chzzkUid, [FromBody] UpdateLiveSettingRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var success = await _apiClient.UpdateLiveSettingAsync(chzzkUid, request, accessToken);
        return Ok(new { success });
    }

    /// <summary>
    /// [?ㅽ듃由???議고쉶]: 諛⑹넚 ?≪텧???꾩슂???ㅽ듃由??ㅻ? 議고쉶?⑸땲??
    /// </summary>
    [HttpGet("{chzzkUid}/stream-key")]
    public async Task<IActionResult> GetStreamKey(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var result = await _apiClient.GetStreamKeyAsync(chzzkUid, accessToken);
        if (result == null) return NotFound("?ㅽ듃由??ㅻ? 議고쉶?????놁뒿?덈떎.");

        return Ok(result);
    }
}
