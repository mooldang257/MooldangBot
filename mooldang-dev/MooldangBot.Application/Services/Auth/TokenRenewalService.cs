using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using MooldangBot.Domain.Models.Chzzk;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;

namespace MooldangBot.Application.Services.Auth;

/// <summary>
/// [끊기지 않는 파동]: 스트리머의 리프레시 토큰을 사용하여 액세스 토큰을 자동으로 갱신하는 서비스입니다.
/// </summary>
public class TokenRenewalService : ITokenRenewalService
{
    private readonly IAppDbContext _db;
    private readonly IChzzkApiClient _chzzkApi; // [오시리스의 수리]: 통합 클라이언트로 교체
    private readonly IChzzkAccessCredentialStore _tokenStore;
    private readonly ILogger<TokenRenewalService> _logger;
    private readonly AsyncRetryPolicy<bool> _retryPolicy;
    private static AsyncCircuitBreakerPolicy<bool>? _circuitBreaker; 
    private static readonly object _initLock = new(); 

    public TokenRenewalService(
        IAppDbContext db,
        IChzzkApiClient chzzkApi,
        IChzzkAccessCredentialStore tokenStore,
        ILogger<TokenRenewalService> logger)
    {
        _db = db;
        _chzzkApi = chzzkApi;
        _tokenStore = tokenStore;
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
        var streamer = await _db.CoreStreamerProfiles
            .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
            
        if (streamer == null) return false;

        // 리프레시 토큰이 아예 없는 경우 자가 치유 불능
        if (string.IsNullOrEmpty(streamer.ChzzkRefreshToken))
        {
            _logger.LogWarning("❌ [영겁의 열쇠] {ChzzkUid} 채널의 리프레시 토큰이 누락되어 복구가 불가능합니다.", chzzkUid);
            return false;
        }

        // 1. 스트리머 본인 토큰 갱신 (지휘관님의 지침에 따라 Gateway 위임)
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

        var kstNow = KstClock.Now;
        var isExpiringSoon = KstClock.IsExpiringSoon(expiresAt, TimeSpan.FromHours(1));
        if (!isExpiringSoon && !force) return true;

        _logger.LogInformation($"[영겁의 열쇠] {streamer.ChzzkUid} 스트리머 토큰 갱신 시도 (Gateway 위임 방식).");

        // [오시리스의 지혜]: 지휘관님의 지침에 따라 게이트웨이 통합 클라이언트에 갱신 전표를 위임합니다.
        // 클라이언트 아이디, 시크릿 등 민감 정보는 이제 게이트웨이가 관리합니다.
        var result = await _chzzkApi.RefreshTokenAsync(refreshToken!);

        if (result == null || string.IsNullOrEmpty(result.AccessToken))
        {
            _logger.LogError("❌ [영겁의 열쇠] {ChzzkUid} 게이트웨이를 통한 토큰 갱신 실패", streamer.ChzzkUid);
            return false;
        }

        streamer.ChzzkAccessToken = result.AccessToken;
        if (!string.IsNullOrEmpty(result.RefreshToken)) streamer.ChzzkRefreshToken = result.RefreshToken;
        streamer.TokenExpiresAt = KstClock.Now.AddSeconds(result.ExpiresIn);

        // 🛡️ [v2.0] Redis 우선 갱신
        await _tokenStore.SetTokenAsync(streamer.ChzzkUid, new ChzzkTokenInfo(
            result.AccessToken,
            streamer.ChzzkRefreshToken ?? "",
            streamer.TokenExpiresAt.Value,
            DateTime.UtcNow
        ));

        await _db.SaveChangesAsync();
        _logger.LogInformation("✅ [영겁의 열쇠] {ChzzkUid} 토큰 갱신 및 동기화 완료 (Gateway -> Redis + DB)", streamer.ChzzkUid);
        return true;
    }

}
