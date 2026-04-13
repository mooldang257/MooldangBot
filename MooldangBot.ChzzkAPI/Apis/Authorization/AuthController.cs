using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Authorization;

namespace MooldangBot.ChzzkAPI.Apis.Authorization;

/// <summary>
/// [오시리스의 관문 - 인증]: 치지직 OAuth2 인증 요청 및 토큰 관리를 처리합니다.
/// </summary>
[ApiController]
[Route("apis/chzzk/auth")]
public class AuthController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IChzzkApiClient apiClient, ILogger<AuthController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [?좏겙 援먰솚]: ?몄쬆 肄붾뱶瑜??ъ슜?섏뿬 ?≪꽭???좏겙??諛쒓툒諛쏆뒿?덈떎.
    /// </summary>
    [HttpPost("token/exchange")]
    public async Task<IActionResult> ExchangeToken([FromQuery] string code, [FromQuery] string state = "mooldang")
    {
        var token = await _apiClient.GetTokenAsync(code, state);
        if (token == null) return BadRequest("?좏겙 諛쒓툒??실패하입니다");

        return Ok(token);
    }

    /// <summary>
    /// [?좏겙 媛깆떊]: 由ы봽?덉떆 ?좏겙???ъ슜?섏뿬 ?≪꽭???좏겙??媛깆떊?⑸땲??
    /// </summary>
    [HttpPost("token/refresh")]
    public async Task<IActionResult> RefreshToken([FromQuery] string refreshToken)
    {
        var token = await _apiClient.RefreshTokenAsync(refreshToken);
        if (token == null) return BadRequest("?좏겙 媛깆떊??실패하입니다");

        return Ok(token);
    }

    /// <summary>
    /// [?좏겙 ?먭린]: ?ъ슜 以묒씤 ?좏겙??臾댄슚?뷀빀?덈떎.
    /// </summary>
    [HttpPost("token/revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        var success = await _apiClient.RevokeTokenAsync(request.Token, request.TokenTypeHint);
        return Ok(new { success });
    }
}
