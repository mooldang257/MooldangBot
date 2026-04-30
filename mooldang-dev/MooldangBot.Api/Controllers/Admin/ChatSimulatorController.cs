using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Contracts.Chzzk.Models.Internal;
using MooldangBot.Domain.Models.Chzzk;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [오시리스의 시뮬레이터]: 방송 채팅을 대신하여 가상 이벤트를 주입하는 관리자 전용 도구입니다.
/// </summary>
[ApiController]
[Route("api/admin/simulator")]
[Authorize]
public class ChatSimulatorController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ChatSimulatorController> logger) : ControllerBase
{
    private readonly string _chzzkBotUrl = configuration["CHZZK_BOT_INTERNAL_URL"] ?? "http://mooldang-dev-chzzk-bot:8080";
    private readonly string _internalSecret = configuration["INTERNAL_API_SECRET"] ?? "";

    /// <summary>
    /// [v3.8] 가상 채팅 이벤트를 봇에 주입합니다.
    /// </summary>
    [HttpPost("inject")]
    public async Task<IActionResult> InjectEvent([FromBody] SimulatorRequest request)
    {
        logger.LogInformation("🧪 [Simulator] Received injection request for {ChzzkUid}: {Content}", request.ChzzkUid, request.Content);
        
        // [이지스 권한 체크]: 본인의 채널에 대해서만 시뮬레이션 가능하게 하거나 관리자 체크
        
        var payload = new ChzzkChatEventPayload
        {
            ChannelId = request.ChzzkUid,
            Content = request.Content,
            SenderChannelId = "simulator_user_id",
            ProfileJson = JsonSerializer.Serialize(new ChzzkChatProfile 
            { 
                Nickname = request.Nickname ?? "Simulator",
                UserRoleCode = "common_user"
            })
        };

        var injectRequest = new InjectEventRequest
        {
            ChzzkUid = request.ChzzkUid,
            EventName = "CHAT",
            RawJson = JsonSerializer.Serialize(payload)
        };

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Secret-Key", _internalSecret);

        try
        {
            var response = await client.PostAsJsonAsync($"{_chzzkBotUrl}/api/internal/test/inject", injectRequest);
            
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("🧪 [Simulator] Event injected for {ChzzkUid}: {Content}", request.ChzzkUid, request.Content);
                return Ok(MooldangBot.Domain.Common.Models.Result<string>.Success("이벤트 주입 성공"));
            }
            
            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("⚠️ [Simulator] Injection failed: {Status} - {Error}", response.StatusCode, error);
            return StatusCode((int)response.StatusCode, MooldangBot.Domain.Common.Models.Result<string>.Failure($"봇 서버 거절: {error}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🚨 [Simulator] Communication error with ChzzkBot");
            return StatusCode(500, MooldangBot.Domain.Common.Models.Result<string>.Failure("치지직 봇 통신 오류가 발생했습니다."));
        }
    }
}

public class SimulatorRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("chzzkUid")]
    public string ChzzkUid { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
