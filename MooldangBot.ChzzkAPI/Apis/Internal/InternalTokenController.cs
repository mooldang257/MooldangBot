using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Chzzk;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Internal;

namespace MooldangBot.ChzzkAPI.Apis.Internal;

/// <summary>
/// [오시리스의 보안 관문]: 메인 봇 엔진으로부터 최신 치지직 토큰을 수신하여 갱신하거나, 인증 대행(Proxy)을 수행합니다.
/// </summary>
[ApiController]
[Route("api/internal/auth")]
public class InternalTokenController : ControllerBase
{
    private readonly IChzzkGatewayTokenStore _tokenStore;
    private readonly IShardedWebSocketManager _shardManager;
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<InternalTokenController> _logger;
    private const string InternalSecretHeader = "X-Internal-Secret-Key";

    public InternalTokenController(
        IChzzkGatewayTokenStore tokenStore, 
        IShardedWebSocketManager shardManager,
        IChzzkApiClient apiClient,
        ILogger<InternalTokenController> logger)
    {
        _tokenStore = tokenStore;
        _shardManager = shardManager;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [오시리스의 대행]: 네이버 서버와 직접 통신하여 인증 코드를 토큰으로 교환합니다.
    /// </summary>
    [HttpPost("exchange-token")]
    public async Task<IActionResult> ExchangeToken([FromBody] ExchangeTokenRequest request)
    {
        if (!IsAuthorized()) return Unauthorized();

        _logger.LogInformation("📡 [Gateway] 통합 클라이언트를 통한 토큰 교환 시도... (State: {State})", request.State);

        // [오시리스의 통합]: 지휘관님의 지침에 따라 통합 클라이언트(_apiClient)로 호출을 일원화합니다.
        // ChzzkApiClient 내부에서 이제 봉투 유무를 자동으로 판단하여 처리합니다.
        var tokenResponse = await _apiClient.ExchangeTokenAsync(request.Code, state: request.State);

        if (tokenResponse == null)
        {
            _logger.LogError("❌ [Gateway] 통합 클라이언트를 통한 토큰 교환 실패");
            return StatusCode(500, "Failed to exchange token via integrated client");
        }

        return Ok(new ChzzkApiResponse<MooldangBot.Contracts.Chzzk.Models.Chzzk.Authorization.TokenResponse>
        {
            Code = 200,
            Content = tokenResponse
        });
    }

    [HttpPost("update-tokens")]
    public async Task<IActionResult> UpdateTokens([FromBody] UpdateTokenRequest request)
    {
        if (!IsAuthorized()) return Unauthorized();

        await _tokenStore.SetTokenAsync(request.ChzzkUid, request.SessionCookie, request.AuthCookie);
        _logger.LogInformation("✅ [Security] 채널 {ChzzkUid}의 토큰이 성공적으로 갱신되었습니다.", request.ChzzkUid);

        // [물멍]: 토큰 갱신 시 즉시 실시간 연결을 시도합니다.
        try
        {
            var session = await _apiClient.GetSessionUrlAsync(request.ChzzkUid, request.AuthCookie);
            if (session != null && !string.IsNullOrEmpty(session.Url))
            {
                await _shardManager.ConnectAsync(request.ChzzkUid, session.Url, request.AuthCookie);
                _logger.LogInformation("🚀 [Realtime] 채널 {ChzzkUid}에 대한 즉각적인 실시간 연결이 수립되었습니다.", request.ChzzkUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [Realtime] 채널 {ChzzkUid} 즉시 연결 실패 (워커가 재접속을 관리함)");
        }

        return Ok();
    }

    private bool IsAuthorized()
    {
        if (!Request.Headers.TryGetValue(InternalSecretHeader, out var secret) || 
            secret != Environment.GetEnvironmentVariable("INTERNAL_API_SECRET"))
        {
            _logger.LogWarning("⚠️ [Security] 미인증 접근 차단");
            return false;
        }
        return true;
    }
}
