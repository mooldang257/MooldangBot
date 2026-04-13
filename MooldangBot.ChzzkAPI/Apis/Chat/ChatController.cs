using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Chat;

namespace MooldangBot.ChzzkAPI.Apis.Chat;

/// <summary>
/// [오시리스의 사령부 - 채팅]: 치지직 채팅 메시지 전송 및 관리를 수행하는 컨트롤러입니다.
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
    /// [메시지 발송]: 특정 채널에 채팅 메시지를 전송합니다.
    /// </summary>
    [HttpPost("{chzzkUid}/send")]
    public async Task<IActionResult> SendMessage(string chzzkUid, [FromBody] SendChatRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var result = await _apiClient.SendChatMessageAsync(chzzkUid, request.Message, accessToken);
        if (result == null) return BadRequest("메시지 전송에 실패했습니다");

        return Ok(result);
    }

    /// <summary>
    /// [공지 설정]: 채팅방 상단 공지를 등록하거나 해제합니다.
    /// </summary>
    [HttpPost("{chzzkUid}/notice")]
    public async Task<IActionResult> SetNotice(string chzzkUid, [FromBody] SetChatNoticeRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var success = await _apiClient.SetChatNoticeAsync(chzzkUid, request, accessToken);
        return Ok(new { success });
    }

    /// <summary>
    /// [메시지 블라인드]: 특정 메시지를 보이지 않게 처리합니다.
    /// </summary>
    [HttpPost("{chzzkUid}/blind")]
    public async Task<IActionResult> BlindMessage(string chzzkUid, [FromBody] BlindMessageRequest request, [FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var success = await _apiClient.BlindMessageAsync(chzzkUid, request, accessToken);
        return Ok(new { success });
    }

    /// <summary>
    /// [채팅방 설정 조회]: 현재 채팅방의 필터링 및 입장 제한 설정을 조회합니다.
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
