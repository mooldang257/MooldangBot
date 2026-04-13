using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Shared;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Users;

namespace MooldangBot.ChzzkAPI.Apis.Internal;

/// <summary>
/// [오시리스의 거울]: 치지직 사용자 정보를 네이버 서버로부터 안전하게 조회하는 내부 프록시 컨트롤러입니다.
/// </summary>
[ApiController]
[Route("api/internal/user")]
public class InternalUserController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<InternalUserController> _logger;
    private const string InternalSecretHeader = "X-Internal-Secret-Key";

    public InternalUserController(IChzzkApiClient apiClient, ILogger<InternalUserController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetUserMe([FromQuery] string token)
    {
        if (!IsAuthorized()) return Unauthorized();

        if (string.IsNullOrEmpty(token)) return BadRequest("Token is required");

        _logger.LogInformation("📡 [Gateway] 네이버 서버로 사용자 정보(UserMe) 조회 요청 발송...");
        
        try
        {
            var userMe = await _apiClient.GetUserMeAsync(token);

            if (userMe == null)
            {
                _logger.LogError("❌ [Gateway] 사용자 정보 조회 실패");
                return NotFound("사용자 정보를 가져올 수 없습니다.");
            }

            // [물멍]: 네이버가 실제로 반환하는 ChannelId를 로그에 찍어 DB와 대조합니다.
            _logger.LogInformation("✅ [Gateway] 사용자 정보 조회 성공 - ChannelId: {Id}, Name: {Name}", userMe.ChannelId, userMe.ChannelName);
            
            return Ok(new ChzzkApiResponse<UserMeResponse> 
            { 
                Code = 200, 
                Content = userMe 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Gateway] 사용자 정보 조회 중 예외 발생");
            return StatusCode(500, ex.Message);
        }
    }

    private bool IsAuthorized()
    {
        if (!Request.Headers.TryGetValue(InternalSecretHeader, out var secret) || 
            secret != Environment.GetEnvironmentVariable("INTERNAL_API_SECRET"))
        {
            return false;
        }
        return true;
    }
}
