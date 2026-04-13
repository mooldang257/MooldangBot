using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Shared;

namespace MooldangBot.ChzzkAPI.Apis.Channels;

/// <summary>
/// [오시리스???곸? - 梨꾨꼸]: 치지직梨꾨꼸 ?뺣낫 諛?愿由ъ옄/?붾줈??愿由щ? 담당하는 컨트롤러?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/channels")]
public class ChannelsController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<ChannelsController> _logger;

    public ChannelsController(IChzzkApiClient apiClient, ILogger<ChannelsController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [梨꾨꼸 ?꾨줈??조회]: 특정 梨꾨꼸??怨듦컻???꾨줈???뺣낫瑜?媛?몄샃?덈떎.
    /// </summary>
    [HttpGet("{chzzkUid}/profile")]
    public async Task<IActionResult> GetProfile(string chzzkUid)
    {
        var profile = await _apiClient.GetChannelProfileAsync(chzzkUid);
        if (profile == null) return NotFound("梨꾨꼸 ?꾨줈?꾩쓣 李얠쓣 ???놁뒿?덈떎.");

        return Ok(profile);
    }

    /// <summary>
    /// [愿由ъ옄 紐⑸줉 조회]: 특정 梨꾨꼸??愿由ъ옄 紐낅떒??조회?⑸땲??
    /// </summary>
    [HttpGet("{chzzkUid}/managers")]
    public async Task<IActionResult> GetManagers(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var managers = await _apiClient.GetManagersAsync(chzzkUid, accessToken);
        return Ok(managers);
    }

    /// <summary>
    /// [?붾줈??紐⑸줉 조회]: 梨꾨꼸 ?붾줈??紐⑸줉???섏씠吏 ?⑥쐞濡?조회?⑸땲??
    /// </summary>
    [HttpGet("{chzzkUid}/followers")]
    public async Task<IActionResult> GetFollowers(string chzzkUid, [FromHeader(Name = "Authorization")] string authHeader, [FromQuery] int size = 20, [FromQuery] int page = 0)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var followers = await _apiClient.GetFollowersAsync(chzzkUid, accessToken, size, page);
        return Ok(followers);
    }
}
