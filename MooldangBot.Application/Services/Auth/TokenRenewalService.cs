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
        if (streamer == null) return false;

        bool success = true;

        // 1. 스트리머 본인 토큰 갱신
        if (!string.IsNullOrEmpty(streamer.ChzzkRefreshToken))
        {
            success &= await RenewTokenInternalAsync(streamer, isBot: false);
        }

        // 2. 봇 계정 토큰 갱신
        if (!string.IsNullOrEmpty(streamer.BotRefreshToken))
        {
            success &= await RenewTokenInternalAsync(streamer, isBot: true);
        }

        return success;
    }

    private async Task<bool> RenewTokenInternalAsync(StreamerProfile streamer, bool isBot)
    {
        var expiresAt = isBot ? streamer.BotTokenExpiresAt : streamer.TokenExpiresAt;
        var refreshToken = isBot ? streamer.BotRefreshToken : streamer.ChzzkRefreshToken;

        // 만료 1시간 전인지 확인
        var isExpiringSoon = expiresAt == null || expiresAt <= DateTime.UtcNow.AddHours(1);
        if (!isExpiringSoon) return true;

        _logger.LogInformation($"[영겁의 열쇠] {streamer.ChzzkUid} {(isBot ? "봇" : "스트리머")} 토큰 갱신 시도. (만료: {expiresAt})");

        // 스트리머 전용 앱 정보 또는 시스템 기본값 사용
        string clientId = streamer.ApiClientId ?? _config["CHZZK_API:CLIENT_ID"] ?? _config["ChzzkApi:ClientId"] ?? "";
        string clientSecret = streamer.ApiClientSecret ?? _config["CHZZK_API:CLIENT_SECRET"] ?? _config["ChzzkApi:ClientSecret"] ?? "";

        using var client = _httpClientFactory.CreateClient();
        var payload = new
        {
            grantType = "refresh_token",
            refreshToken = refreshToken,
            clientId = clientId,
            clientSecret = clientSecret
        };

        var response = await client.PostAsJsonAsync(TokenUrl, payload);
        if (!response.IsSuccessStatusCode)
        {
            var errorDetail = await response.Content.ReadAsStringAsync();
            _logger.LogError($"[영겁의 열쇠] {streamer.ChzzkUid} {(isBot ? "봇" : "스트리머")} 갱신 실패 (HTTP {response.StatusCode}): {errorDetail}");
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>();
        if (result == null || result.Content == null || string.IsNullOrEmpty(result.Content.AccessToken)) return false;

        var content = result.Content;
        if (isBot)
        {
            streamer.BotAccessToken = content.AccessToken;
            if (!string.IsNullOrEmpty(content.RefreshToken)) streamer.BotRefreshToken = content.RefreshToken;
            streamer.BotTokenExpiresAt = DateTime.UtcNow.AddSeconds(content.ExpiresIn);
        }
        else
        {
            streamer.ChzzkAccessToken = content.AccessToken;
            if (!string.IsNullOrEmpty(content.RefreshToken)) streamer.ChzzkRefreshToken = content.RefreshToken;
            streamer.TokenExpiresAt = DateTime.UtcNow.AddSeconds(content.ExpiresIn);
        }

        await _db.SaveChangesAsync();
        return true;
    }

}
