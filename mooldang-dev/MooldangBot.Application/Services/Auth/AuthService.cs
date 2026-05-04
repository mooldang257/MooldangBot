using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Distributed;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Events;
using MediatR;

using Microsoft.AspNetCore.DataProtection;

namespace MooldangBot.Application.Services.Auth;

/// <summary>
/// [오시리스의 전령]: 사용자 인증 및 권한 관리의 핵심 비즈니스 로직을 담당합니다.
/// (Aegis of Identity): 모든 민감 정보는 IDataProtector을 통해 보호됩니다.
/// </summary>
public class AuthService(
    IAppDbContext _db,
    IChzzkApiClient _chzzkApi,
    IConfiguration _configuration,
    IChzzkBotService _botService,
    IDistributedCache _cache,
    IDataProtectionProvider _protectionProvider,
    IChzzkAccessCredentialStore _tokenStore, // [오시리스의 영겁]: Redis 토큰 저장소 추가
    IMediator _mediator, // [오시리스의 지혜]: 이벤트 발행을 위한 메디에이터 추가
    IIdentityCacheService _identityCache, // [이지스의 눈]: 시청자 캐시 동기화 추가
    ILogger<AuthService> _logger) : IAuthService
{
    private const string CacheKeyPrefix = "OverlayTokenVersion:";
    private readonly IDataProtector _tokenProtector = _protectionProvider.CreateProtector("MooldangBot.Aegis.v1");

    // [v5.0] 클레임 명칭 상수화
    private const string ClaimStreamerId = "StreamerId";
    private const string ClaimTokenVersion = "TokenVersion";

    private string BaseDomain 
    {
        get {
            var val = _configuration["BASE_DOMAIN"];
            if (string.IsNullOrEmpty(val))
                throw new Exception("BASE_DOMAIN이 설정되어 있지 않습니다.");
            
            if (!val.StartsWith("http://") && !val.StartsWith("https://"))
            {
                val = "https://" + val;
            }
            
            return val;
        }
    }

    public async Task<AuthMetadata> GenerateAuthMetadataAsync(string? targetUid = null, string? loginType = null)
    {
        // 1. 시스템 ClientId 조회 (환경 변수 우선 시스템으로 전향)
        string clientId = _configuration["CHZZK_API:CLIENT_ID"] 
                         ?? _configuration["ChzzkApi:ClientId"] 
                         ?? throw new Exception("Chzzk Client ID가 설정되어 있지 않습니다. (.env 확인 필요)");

        // 2. State 생성 (PKCE 제거)
        string state = Guid.NewGuid().ToString("N");

        // 3. 인증 URL 구성 (전역 환경 변수 CHZZK_REDIRECT_URI 우선 사용)
        string redirectUri = _configuration["CHZZK_REDIRECT_URI"] 
                            ?? ( (!string.IsNullOrEmpty(_configuration["ChzzkApi:RedirectUri"])) 
                                 ? $"{_configuration["ChzzkApi:RedirectUri"]?.TrimEnd('/')}/Auth/callback" 
                                 : $"{BaseDomain.TrimEnd('/')}/Auth/callback" );
        
        string encodedRedirect = System.Net.WebUtility.UrlEncode(redirectUri);
        
        // 치지직 표준 AUTH URL (PKCE 파라미터 제거)
        string authUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={encodedRedirect}&state={state}";

        _logger.LogInformation($"[오시리스의 전령] 신규 인증 요청 생성 (State: {state}, Type: {loginType})");
        
        return new AuthMetadata(authUrl, state, "");
    }

    public async Task<AuthResult> ProcessCallbackAsync(string code, AuthSessionData cachedData)
    {
        try
        {
            // [오시리스 v10.5]: 전역 환경 변수에서 리다이렉트 URL 로드
            string redirectUri = _configuration["CHZZK_REDIRECT_URI"] 
                                ?? ( (!string.IsNullOrEmpty(_configuration["ChzzkApi:RedirectUri"])) 
                                     ? $"{_configuration["ChzzkApi:RedirectUri"]?.TrimEnd('/')}/Auth/callback" 
                                     : $"{BaseDomain.TrimEnd('/')}/Auth/callback" );

            // 1. [오시리스 v10.5]: 통합 서비스를 통한 토큰 교환 (정규화된 RedirectUri 전달)
            var tokenResult = await _chzzkApi.ExchangeTokenAsync(code, state: cachedData.State, redirectUri: redirectUri);
            
            // [v2.6] 필수 토큰 유효성 검증 강화 (CS8604 등 대응)
            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken) || string.IsNullOrEmpty(tokenResult.RefreshToken))
            {
                _logger.LogError("[인증] 토큰 교환 실패 또는 필수 토큰 정보가 누락되었습니다.");
                return new AuthResult { IsSuccess = false, ErrorMessage = "인증 토큰 정보가 불완전합니다." };
            }

            _logger.LogInformation($"[인증] 토큰 교환 성공 (ExpiresIn: {tokenResult.ExpiresIn})");
            var expireDate = KstClock.Now.AddSeconds(tokenResult.ExpiresIn);

            // 2. 봇 설정 흐름 처리 (v17.0 이후 레거시 대응: 개별 스트리머 프로필로 통합됨)
            if (cachedData.State.StartsWith("bot_setup_") || !string.IsNullOrEmpty(cachedData.TargetUid))
            {
                _logger.LogWarning("[오시리스의 경고] 레거시 봇 설정 흐름이 감지되었습니다. 봇 권한은 스트리머 프로필 동기화로 통합되었습니다.");
                // 봇 설정 페이지로 리다이렉트하거나 일반 스트리머 동기화로 유도
                return new AuthResult { IsSuccess = true, ChannelName = "Legacy Bot Setup", RedirectUrl = "/dashboard" };
            }

            // 3. [오시리스 v10.1]: 통합 서비스를 통한 사용자 정보 조회
            var userMeResult = await _chzzkApi.GetUserMeAsync(tokenResult.AccessToken);
            if (userMeResult == null)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "사용자 정보 조회 실패 또는 응답 데이터 없음" };
            }

            string chzzkUid = userMeResult.ChannelId.ToLower();
            string channelName = userMeResult.ChannelName ?? "알 수 없음";

            // 4. 역할별 동기화 분기 (v6.2.3)
            if (cachedData.LoginType == "viewer")
            {
                return await SyncGlobalViewerAsync(chzzkUid, channelName, userMeResult.ChannelImageUrl);
            }
            else
            {
                var result = await SyncStreamerProfileAsync(chzzkUid, channelName, userMeResult.ChannelImageUrl, tokenResult.AccessToken, tokenResult.RefreshToken, expireDate);
                
                // [v17.0] 봇 서비스 복구 락 해제
                _botService.CleanupRecoveryLock(chzzkUid);
                
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[오시리스의 실책] 인증 콜백 처리 중 오류 발생");
            return new AuthResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<AuthResult> SyncGlobalViewerAsync(string chzzkUid, string channelName, string? profileImageUrl)
    {
        // [이지스 통합]: 시청자 동기화 및 캐싱을 IdentityCacheService로 일원화합니다.
        await _identityCache.SyncGlobalViewerIdAsync(chzzkUid, channelName, profileImageUrl);

        _logger.LogInformation($"[오시리스의 기록] 시청자 정보 동기화 완료 (ChzzkUid: {chzzkUid}, Nickname: {channelName})");
        return new AuthResult { IsSuccess = true, ChzzkUid = chzzkUid, ChannelName = channelName };
    }

    // [v18.0] HandleBotSetupAsync 제거됨 (SystemSetting 테이블 폐기에 따름)

    private async Task<AuthResult> SyncStreamerProfileAsync(string chzzkUid, string channelName, string? profileImageUrl, string accessToken, string refreshToken, KstClock? expireDate)
    {
        // [물멍]: 네이버 공식 Channel ID를 기반으로 프로필을 색인합니다.
        var streamer = await _db.TableCoreStreamerProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        bool isNewStreamer = false; // [오시리스의 눈]: 신입 대원 판별 플래그
        
        if (streamer == null)
        {
            isNewStreamer = true;
            _logger.LogInformation("📡 [인증] 신규 스트리머 채널 등록 시작 (ChannelId: {ChannelId})", chzzkUid);
            streamer = new CoreStreamerProfiles 
            { 
                ChzzkUid = chzzkUid,
                Slug = chzzkUid, 
                IsActive = true,
                IsMasterEnabled = true,
                CreatedAt = KstClock.Now
            };
            _db.TableCoreStreamerProfiles.Add(streamer);

            _db.TableFuncSongListSessions.Add(new FuncSongListSessions 
            { 
                CoreStreamerProfiles = streamer, 
                StartedAt = KstClock.Now,
                IsActive = true 
            });
        }

        // [오시리스의 인장]: 항상 최신 ChannelName과 프로필 이미지를 반영합니다.
        streamer.ChannelName = channelName;
        streamer.ProfileImageUrl = profileImageUrl;
        streamer.ChzzkAccessToken = accessToken;
        streamer.ChzzkRefreshToken = refreshToken;
        streamer.TokenExpiresAt = expireDate;

        // [물멍]: 슬러그가 비어있는 경우 채널 ID를 기본값으로 할당합니다.
        if (string.IsNullOrEmpty(streamer.Slug))
        {
            streamer.Slug = chzzkUid;
        }

        await _db.SaveChangesAsync();

        // 🛡️ [v2.4.7] 오시리스의 실시간 동기화: DB 저장 직후 Redis 캐시를 강제로 갱신하여 봇 엔진(Gateway)과 동기화합니다.
        await _tokenStore.SetTokenAsync(chzzkUid, new ChzzkTokenInfo(
            accessToken,
            refreshToken,
            expireDate?.Value ?? DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow
        ));

        // [오시리스의 축복]: 신규 스트리머 등록 이벤트 발행 (비동기 명령어/룰렛 시딩 트리거)
        // [지휘관님의 지침]: DB 커밋이 완료된 후 핸들러를 가동하여 데이터 정합성을 보장합니다.
        if (isNewStreamer)
        {
            await _mediator.Publish(new StreamerRegisteredEvent(chzzkUid, channelName));
        }

        _logger.LogInformation($"✅ [인증] 스트리머 프로필 동기화 완료 (ChannelId: {chzzkUid}, Slug: {streamer.Slug})");

        return new AuthResult { IsSuccess = true, ChzzkUid = chzzkUid, ChannelName = channelName, Slug = streamer.Slug };
    }

    public void CleanupRecoveryLock(string chzzkUid) => _botService.CleanupRecoveryLock(chzzkUid);

    public async Task<string> IssueOverlayTokenAsync(string chzzkUid, string role)
    {
        var streamer = await _db.TableCoreStreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (streamer == null) throw new Exception("[오시리스의 거절] 해당 스트리머를 찾을 수 없습니다.");

        // [오시리스의 정제]: URL 가독성을 위해 16자리 짧은 해시 토큰을 생성하거나 반환합니다.
        if (string.IsNullOrEmpty(streamer.OverlayToken))
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new char[16];
            for (int i = 0; i < 16; i++)
            {
                random[i] = chars[Random.Shared.Next(chars.Length)];
            }
            streamer.OverlayToken = new string(random);
            await _db.SaveChangesAsync();
            
            _logger.LogInformation($"[오시리스의 인장] 신규 오버레이 짧은 토큰이 발급되었습니다. (ChzzkUid: {chzzkUid})");
        }
        
        return streamer.OverlayToken;
    }

    public async Task<bool> RevokeOverlayTokenAsync(string chzzkUid)
    {
        var streamer = await _db.TableCoreStreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (streamer == null) return false;

        // [오시리스의 철퇴]: 버전을 올림으로써 기존 JWT 토큰들을 무효화
        streamer.OverlayTokenVersion++;

        // [오시리스의 인장 갱신]: 짧은 해시 토큰도 새로 생성하여 기존 주소를 무효화합니다.
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new char[16];
        for (int i = 0; i < 16; i++)
        {
            random[i] = chars[Random.Shared.Next(chars.Length)];
        }
        streamer.OverlayToken = new string(random);

        await _db.SaveChangesAsync();

        // 🛡️ [오시리스의 기억 소멸]: 캐시 무효화 (불일치 방지)
        string cacheKey = $"{CacheKeyPrefix}{chzzkUid}";
        await _cache.RemoveAsync(cacheKey);

        _logger.LogWarning($"[오시리스의 철퇴] 오버레이 토큰 폐기 및 짧은 해시 재발급 완료 (ChzzkUid: {chzzkUid}, NewVersion: {streamer.OverlayTokenVersion})");
        
        return true;
    }
}
