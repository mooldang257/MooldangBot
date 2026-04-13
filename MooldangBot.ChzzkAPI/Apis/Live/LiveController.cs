using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Live;

namespace MooldangBot.ChzzkAPI.Apis.Live;

/// <summary>
/// [오시리스의 눈 - 라이브]: 실시간 방송 상태 및 스트림 정보를 조회합니다.
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
    /// [諛⑹넚 설정 조회]: 현재 梨꾨꼸??諛⑹넚 ?쒕ぉ, 移댄뀒怨좊━ ?깆쓣 조회?⑸땲??
    /// </summary>
    [HttpGet("{chzzkUid}/settings")]
    public async Task<IActionResult> GetSettings(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var settings = await _apiClient.GetLiveSettingAsync(chzzkUid, accessToken);
        if (settings == null) return NotFound("諛⑹넚 설정??媛?몄삤吏 紐삵뻽입니다");

        return Ok(settings);
    }

    /// <summary>
    /// [諛⑹넚 설정 ?낅뜲?댄듃]: 諛⑹넚 ?쒕ぉ, 移댄뀒怨좊━ ?깆쓣 蹂寃쏀빀?덈떎.
    /// </summary>
    [HttpPatch("{chzzkUid}/settings")]
    public async Task<IActionResult> UpdateSettings(string chzzkUid, [FromBody] UpdateLiveSettingRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var success = await _apiClient.UpdateLiveSettingAsync(chzzkUid, request, accessToken);
        return Ok(new { success });
    }

    /// <summary>
    /// [?ㅽ듃由???조회]: 諛⑹넚 ?≪텧???꾩슂???ㅽ듃由??ㅻ? 조회?⑸땲??
    /// </summary>
    [HttpGet("{chzzkUid}/stream-key")]
    public async Task<IActionResult> GetStreamKey(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var result = await _apiClient.GetStreamKeyAsync(chzzkUid, accessToken);
        if (result == null) return NotFound("?ㅽ듃由??ㅻ? 조회?????놁뒿?덈떎.");

        return Ok(result);
    }
}
