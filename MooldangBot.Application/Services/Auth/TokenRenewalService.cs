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
using MooldangBot.ChzzkAPI.Models;

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
    private static readonly object _initLock = new(); // [v17.0] 초기화 경합 방지용 락 객체

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
        // [v17.0] 널 병합 대입 시 발생할 수 있는 정적 초기화 경합(Race Condition)을 원천 차단하기 위해 Double-check locking 패턴을 사용합니다.
        if (_circuitBreaker == null)
        {
            lock (_initLock)
            {
                _circuitBreaker ??= Policy
                    .Handle<Exception>(ex => ex is not FatalTokenException)
                    .OrResult<bool>(r => r == false)
                    .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
                        onBreak: (ex, breakDelay) => _logger.LogWarning($"[서킷 브레이커] 회로 차단됨! ({breakDelay.TotalSeconds}초 동안 휴식)"),
                        onReset: () => _logger.LogInformation("[서킷 브레이커] 회로 복구됨."),
                        onHalfOpen: () => _logger.LogInformation("[서킷 브레이커] 회로 반개방 상태."));
            }
        }

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
        bool streamerSuccess = await RenewTokenInternalAsync(streamer, force: force);
        if (!streamerSuccess)
        {
            _logger.LogError("❌ [영겁의 열쇠] {ChzzkUid} 스트리머 본인 토큰 갱신 실패로 자가 치유를 중단합니다.", chzzkUid);
            return false;
        }

        return true;
    }

    private async Task<bool> RenewTokenInternalAsync(StreamerProfile streamer, bool force)
    {
        var expiresAt = streamer.TokenExpiresAt;
        var refreshToken = streamer.ChzzkRefreshToken;

        // [v13.1] 모든 비교를 KST 강제 (UTC+9)
        var kstNow = KstClock.Now;
        var isExpiringSoon = KstClock.IsExpiringSoon(expiresAt, TimeSpan.FromHours(1));
        if (!isExpiringSoon && !force) return true;

        _logger.LogInformation($"[영겁의 열쇠] {streamer.ChzzkUid} 스트리머 토큰 갱신 시도. (강제: {force}, 만료: {expiresAt})");

        // [v6.2] 개별 앱 정보 필드 삭제에 따라 시스템 기본값만 사용
        string clientId = _config["CHZZK_API:CLIENT_ID"] ?? _config["ChzzkApi:ClientId"] ?? "";
        string clientSecret = _config["CHZZK_API:CLIENT_SECRET"] ?? _config["ChzzkApi:ClientSecret"] ?? "";

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
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && 
                    errorDetail.Contains("INVALID_TOKEN"))
                {
                    _logger.LogCritical($"❌ [영겁의 열쇠] {streamer.ChzzkUid} 채널의 리프레시 토큰이 영구 무효화되었습니다.");
                    throw new FatalTokenException("INVALID_TOKEN detected");
                }

                // [v17.2 / P1] 진단 로그 강화 (헤더 + 마스킹)
                _logger.LogError(
                    "[영겁의 열쇠] {ChzzkUid} 클라이언트 에러 (HTTP {StatusCode})\n" +
                    "  Client-Id: {ClientId}\n" +
                    "  RefreshToken: {TokenPrefix}...\n" +
                    "  Response: {ErrorDetail}",
                    streamer.ChzzkUid,
                    statusCode,
                    clientId.Length > 5 ? clientId[..5] + "***" : "EMPTY",
                    refreshToken?.Length > 8 ? refreshToken[..8] + "***" : "EMPTY",
                    errorDetail);
                
                return false;
            }

            // 🟡 5xx 서버 에러: 치지직 시스템 장애 등 일시적 현상
            _logger.LogWarning(
                "[영겁의 열쇠] {ChzzkUid} 서버 에러 (HTTP {StatusCode})\n" +
                "  Polly 재시도 대상입니다.\n" +
                "  Response: {ErrorDetail}",
                streamer.ChzzkUid,
                statusCode,
                errorDetail);
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>();
        if (result == null || result.Content == null || string.IsNullOrEmpty(result.Content.AccessToken)) return false;

        var content = result.Content;
        streamer.ChzzkAccessToken = content.AccessToken;
        if (!string.IsNullOrEmpty(content.RefreshToken)) streamer.ChzzkRefreshToken = content.RefreshToken;
        streamer.TokenExpiresAt = KstClock.Now.AddSeconds(content.ExpiresIn);

        await _db.SaveChangesAsync();
        return true;
    }

}
