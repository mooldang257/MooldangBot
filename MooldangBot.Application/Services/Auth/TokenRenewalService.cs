using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
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

    // 치지직 인증 서버 엔드포인트
    private const string TokenUrl = "https://nid.naver.com/oauth2.0/token";

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

        // 2. [서기의 기록]: 리프레시 토큰으로 새 토큰 요청
        using var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", streamer.ChzzkRefreshToken },
            { "client_id", _config["ChzzkApi:ClientId"] ?? "" },
            { "client_secret", _config["ChzzkApi:ClientSecret"] ?? "" }
        });

        var response = await client.PostAsync(TokenUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"[영겁의 열쇠] 갱신 실패: {response.StatusCode}");
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (result == null || string.IsNullOrEmpty(result.AccessToken)) return false;

        // 3. [파동의 부활]
        streamer.ChzzkAccessToken = result.AccessToken;
        if (!string.IsNullOrEmpty(result.RefreshToken)) streamer.ChzzkRefreshToken = result.RefreshToken;
        streamer.TokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn);

        await _db.SaveChangesAsync();
        return true;
    }

    private record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType);
}
