using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.DTOs;
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
        _circuitBreaker ??= Policy
            .Handle<Exception>()
            .OrResult<bool>(r => r == false) // 제너릭 핸들링
            .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
                onBreak: (ex, breakDelay) => _logger.LogWarning($"[서킷 브레이커] 회로 차단됨! ({breakDelay.TotalSeconds}초 동안 휴식)"),
                onReset: () => _logger.LogInformation("[서킷 브레이커] 회로 복구됨."),
                onHalfOpen: () => _logger.LogInformation("[서킷 브레이커] 회로 반개방 상태."));

        // [재시도의 인내]: 2회 재시도 (Exponential Backoff)
        _retryPolicy = Policy<bool>
            .Handle<Exception>()
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
            await _circuitBreaker!.ExecuteAsync(async () => await ProcessRenewalAsync(chzzkUid)));
    }

    public bool IsCircuitOpen() => _circuitBreaker is { CircuitState: CircuitState.Open or CircuitState.Isolated };

    private async Task<bool> ProcessRenewalAsync(string chzzkUid)
    {
        var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
        if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkRefreshToken))
        {
            return false;
        }

        // 1. [만료의 예감]: 만료 1시간 전(또는 이미 만료됨)인지 확인
        var isExpiringSoon = streamer.TokenExpiresAt == null || 
                             streamer.TokenExpiresAt <= DateTime.UtcNow.AddHours(1);

        if (!isExpiringSoon) return true;

        _logger.LogInformation($"[영겁의 열쇠] {chzzkUid} 스트리머의 토큰 임박 감지. (만료: {streamer.TokenExpiresAt})");

        // 2. [서기의 기록]: 리프레시 토큰으로 새 토큰 요청 (JSON 규격 준수)
        using var client = _httpClientFactory.CreateClient();
        var payload = new
        {
            grantType = "refresh_token",
            refreshToken = streamer.ChzzkRefreshToken,
            clientId = _config["CHZZK_API:CLIENT_ID"] ?? _config["ChzzkApi:ClientId"] ?? "",
            clientSecret = _config["CHZZK_API:CLIENT_SECRET"] ?? _config["ChzzkApi:ClientSecret"] ?? ""
        };
        
        var response = await client.PostAsJsonAsync(TokenUrl, payload);
        if (!response.IsSuccessStatusCode)
        {
            var errorDetail = await response.Content.ReadAsStringAsync();
            _logger.LogError($"[영겁의 열쇠] 갱신 실패 (HTTP {response.StatusCode}): {errorDetail}");
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>(); // 공용 DTO 사용
        if (result == null || result.Content == null || string.IsNullOrEmpty(result.Content.AccessToken)) return false;

        // 3. [파동의 부활]
        var content = result.Content;
        streamer.ChzzkAccessToken = content.AccessToken;
        if (!string.IsNullOrEmpty(content.RefreshToken)) streamer.ChzzkRefreshToken = content.RefreshToken;
        streamer.TokenExpiresAt = DateTime.UtcNow.AddSeconds(content.ExpiresIn);

        await _db.SaveChangesAsync();
        return true;
    }

}
