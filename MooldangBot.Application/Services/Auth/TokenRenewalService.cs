using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;

namespace MooldangBot.Application.Services.Auth;

/// <summary>
/// [끊기지 않는 파동]: 스트리머의 리프레시 토큰을 사용하여 액세스 토큰을 자동으로 갱신하는 서비스입니다.
/// </summary>
public class TokenRenewalService : ITokenRenewalService
{
    private readonly IAppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TokenRenewalService> _logger;
    private readonly AsyncRetryPolicy<bool> _retryPolicy;
    private static AsyncCircuitBreakerPolicy<bool>? _circuitBreaker; // [제너릭 전환]

    public TokenRenewalService(
        IAppDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TokenRenewalService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;

        // [서킷 브레이커의 지혜]: 3번 연속 실패 시 30초간 회로 차단
        // [v17.0] FatalTokenException(리프레시 토큰 영구 무효화)은 재시도 불가 → 즉시 전파
        _circuitBreaker ??= Policy
            .Handle<Exception>(ex => ex is not FatalTokenException)
            .OrResult<bool>(r => r == false) // 제너릭 핸들링
            .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
                onBreak: (ex, breakDelay) => _logger.LogWarning($"[서킷 브레이커] 회로 차단됨! ({breakDelay.TotalSeconds}초 동안 휴식)"),
                onReset: () => _logger.LogInformation("[서킷 브레이커] 회로 복구됨."),
                onHalfOpen: () => _logger.LogInformation("[서킷 브레이커] 회로 반개방 상태."));

        // [재시도의 인내]: 2회 재시도 (Exponential Backoff)
        // [v17.0] FatalTokenException은 재시도 제외 — 영구 무효화된 토큰에 대한 무의미한 재시도 차단
        _retryPolicy = Policy<bool>
            .Handle<Exception>(ex => ex is not FatalTokenException)
            .OrResult(result => result == false)
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (result, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"[영겁의 열쇠] 갱신 실패. {timeSpan.TotalSeconds}초 후 {retryCount}회차 재시도합니다.");
                });
    }

    // 치지직 Open API 전역 토큰 엔드포인트
    private const string TokenUrl = "https://openapi.chzzk.naver.com/auth/v1/token";

    public async Task<bool> RenewIfNeededAsync(string chzzkUid)
    {
        // Polly 정책 결합 실행
        return await _retryPolicy.ExecuteAsync(async () => 
            await _circuitBreaker!.ExecuteAsync(async () => await ProcessRenewalAsync(chzzkUid, force: false)));
    }

    public async Task<bool> RenewNowAsync(string chzzkUid)
    {
        // [영겁의 열쇠]: 강제 갱신 프로토콜 가동 (시간 체크 우회)
        return await _retryPolicy.ExecuteAsync(async () => 
            await _circuitBreaker!.ExecuteAsync(async () => await ProcessRenewalAsync(chzzkUid, force: true)));
    }

    public bool IsCircuitOpen() => _circuitBreaker is { CircuitState: CircuitState.Open or CircuitState.Isolated };

    private async Task<bool> ProcessRenewalAsync(string chzzkUid, bool force)
    {
        // [v10.0] AsNoTracking을 제거하여 갱신된 토큰이 DB에 실제 저장되도록 복구
        var streamer = await _db.StreamerProfiles
            .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
            
        if (streamer == null) return false;

        // 리프레시 토큰이 아예 없는 경우 자가 치유 불능
        if (string.IsNullOrEmpty(streamer.ChzzkRefreshToken))
        {
            _logger.LogWarning("❌ [영겁의 열쇠] {ChzzkUid} 채널의 리프레시 토큰이 누락되어 복구가 불가능합니다.", chzzkUid);
            return false;
        }

        // 1. 스트리머 본인 토큰 갱신 (채팅 연결 핵심 - 실패 시 즉시 중단)
        bool streamerSuccess = await RenewTokenInternalAsync(streamer, isBot: false, force: force);
        if (!streamerSuccess)
        {
            _logger.LogError("❌ [영겁의 열쇠] {ChzzkUid} 스트리머 본인 토큰 갱신 실패로 자가 치유를 중단합니다.", chzzkUid);
            return false;
        }

        // 2. 봇 계정 토큰 갱신 (선택 사항)
        if (!string.IsNullOrEmpty(streamer.BotRefreshToken))
        {
             await RenewTokenInternalAsync(streamer, isBot: true, force: force);
        }

        return true;
    }

    private async Task<bool> RenewTokenInternalAsync(StreamerProfile streamer, bool isBot, bool force)
    {
        var expiresAt = isBot ? streamer.BotTokenExpiresAt : streamer.TokenExpiresAt;
        var refreshToken = isBot ? streamer.BotRefreshToken : streamer.ChzzkRefreshToken;

        // [v13.1] 모든 비교를 KST 강제 (UTC+9)
        var kstNow = DateTime.UtcNow.AddHours(9);
        var isExpiringSoon = expiresAt == null || expiresAt <= kstNow.AddHours(1);
        if (!isExpiringSoon && !force) return true;

        _logger.LogInformation($"[영겁의 열쇠] {streamer.ChzzkUid} {(isBot ? "봇" : "스트리머")} 토큰 갱신 시도. (강제: {force}, 만료: {expiresAt})");

        // 스트리머 전용 앱 정보 또는 시스템 기본값 사용
        string clientId = streamer.ApiClientId ?? _config["CHZZK_API:CLIENT_ID"] ?? _config["ChzzkApi:ClientId"] ?? "";
        string clientSecret = streamer.ApiClientSecret ?? _config["CHZZK_API:CLIENT_SECRET"] ?? _config["ChzzkApi:ClientSecret"] ?? "";

        using var client = _httpClientFactory.CreateClient();
        
        // [v16.3.1] 치지직 API는 본문뿐만 아니라 HTTP 헤더에도 인증 정보를 강력하게 요구합니다.
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        client.DefaultRequestHeaders.Add("Client-Id", clientId);
        client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);

        var payload = new
        {
            grantType = "refresh_token",
            refreshToken = refreshToken,
            clientId = clientId,
            clientSecret = clientSecret
        };

        var response = await client.PostAsJsonAsync(TokenUrl, payload);
        var errorDetail = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;

            // 🔴 4xx 클라이언트 에러: 재시도 무의미 (헤더/값 오류)
            if (statusCode is >= 400 and < 500)
            {
                // [v16.3.1] INVALID_TOKEN (401)은 리프레시 토큰이 영구적으로 죽었음을 의미합니다.
                // 무의미한 재시도를 즉각 중단하고 시스템에 알리는 Fatal 에러를 발생시킵니다.
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && 
                    errorDetail.Contains("INVALID_TOKEN"))
                {
                    _logger.LogCritical($"❌ [영겁의 열쇠] {streamer.ChzzkUid} 채널의 리프레시 토큰이 영구 무효화되었습니다.");
                    throw new FatalTokenException("INVALID_TOKEN detected");
                }

                // [v17.1 / P1] 400 Bad Request, 403 Forbidden 등은 클라이언트 측 파라미터/인증 정보 문제 가능성 높음
                _logger.LogError($"[영겁의 열쇠] {streamer.ChzzkUid} 클라이언트 에러 (HTTP {statusCode}). " +
                    $"헤더/페이로드를 점검하세요: {errorDetail}");
                
                // 클라이언트 에러는 재시도해도 결과가 같을 가능성이 높으므로 false 반환 (Polly는 OrResult(false)를 통해 종료)
                return false;
            }

            // 🟡 5xx 서버 에러: 치지직 시스템 장애 등 일시적 현상일 가능성 (재시도 가치 있음)
            _logger.LogWarning($"[영겁의 열쇠] {streamer.ChzzkUid} 서버 에러 (HTTP {statusCode}). " +
                $"Polly 재시도 대상입니다: {errorDetail}");
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>();
        if (result == null || result.Content == null || string.IsNullOrEmpty(result.Content.AccessToken)) return false;

        var content = result.Content;
        if (isBot)
        {
            streamer.BotAccessToken = content.AccessToken;
            if (!string.IsNullOrEmpty(content.RefreshToken)) streamer.BotRefreshToken = content.RefreshToken;
            streamer.BotTokenExpiresAt = DateTime.UtcNow.AddHours(9).AddSeconds(content.ExpiresIn);
        }
        else
        {
            streamer.ChzzkAccessToken = content.AccessToken;
            if (!string.IsNullOrEmpty(content.RefreshToken)) streamer.ChzzkRefreshToken = content.RefreshToken;
            streamer.TokenExpiresAt = DateTime.UtcNow.AddHours(9).AddSeconds(content.ExpiresIn);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetSessionAuthAsync(string chzzkUid)
    {
        var streamer = await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
        if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkAccessToken)) return null;

        string clientId = streamer.ApiClientId ?? _config["CHZZK_API:CLIENT_ID"] ?? _config["ChzzkApi:ClientId"] ?? "";
        string clientSecret = streamer.ApiClientSecret ?? _config["CHZZK_API:CLIENT_SECRET"] ?? _config["ChzzkApi:ClientSecret"] ?? "";

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", streamer.ChzzkAccessToken);
        
        // [v16.6] 채팅 세션 인증 단계에서도 클라이언트 신분 증명을 확실히 요구합니다. 🛡️
        if (!string.IsNullOrEmpty(clientId)) client.DefaultRequestHeaders.Add("Client-Id", clientId);
        if (!string.IsNullOrEmpty(clientSecret)) client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);

        var authUrl = $"https://openapi.chzzk.naver.com/open/v1/chats/access-token?channelId={chzzkUid}";
        try
        {
            var response = await client.GetAsync(authUrl);
            var content = await response.Content.ReadFromJsonAsync<ChzzkChatAuthResponse>();
            return content?.Content?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[영겁의 열쇠] {chzzkUid} 채팅 세션 인증 키 획득 중 오류 발생");
            return null;
        }
    }
}
