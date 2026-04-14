using MooldangBot.Contracts.Chzzk.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Security;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Distributed;
using MooldangBot.Contracts.Models.Chzzk;

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
    ILogger<AuthService> _logger) : IAuthService
{
    private const string CacheKeyPrefix = "OverlayTokenVersion:";
    private readonly IDataProtector _tokenProtector = _protectionProvider.CreateProtector("MooldangBot.Aegis.v1");

    // [v5.0] 클레임 명칭 상수화
    private const string ClaimStreamerId = "StreamerId";
    private const string ClaimTokenVersion = "TokenVersion";

    private string BaseDomain => _configuration["BASE_DOMAIN"] 
        ?? throw new Exception("BASE_DOMAIN이 설정되어 있지 않습니다.");

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
            
            // [v2.6] 필수 토큰 유효성 검증 강화 (CS8604 대응)
            if (string.IsNullOrEmpty(tokenResult.AccessToken) || string.IsNullOrEmpty(tokenResult.RefreshToken))
            {
                _logger.LogError("[인증] 토큰 교환 성공했으나 필수 토큰 정보가 누락되었습니다.");
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
        var viewerHash = Sha256Hasher.ComputeHash(chzzkUid);
        var viewer = await _db.GlobalViewers.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.ViewerUidHash == viewerHash);

        if (viewer == null)
        {
            viewer = new GlobalViewer 
            { 
                ViewerUid = chzzkUid,
                ViewerUidHash = viewerHash,
                Nickname = channelName,
                ProfileImageUrl = profileImageUrl,
                CreatedAt = KstClock.Now
            };
            _db.GlobalViewers.Add(viewer);
        }
        else
        {
            viewer.Nickname = channelName;
            viewer.ProfileImageUrl = profileImageUrl;
            viewer.UpdatedAt = KstClock.Now;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation($"[오시리스의 기록] 시청자 정보 동기화 완료 (ChzzkUid: {chzzkUid}, Nickname: {channelName})");
        return new AuthResult { IsSuccess = true, ChzzkUid = chzzkUid, ChannelName = channelName };
    }

    // [v18.0] HandleBotSetupAsync 제거됨 (SystemSetting 테이블 폐기에 따름)

    private async Task<AuthResult> SyncStreamerProfileAsync(string chzzkUid, string channelName, string? profileImageUrl, string accessToken, string refreshToken, KstClock? expireDate)
    {
        // [물멍]: 네이버 공식 Channel ID를 기반으로 프로필을 색인합니다.
        var streamer = await _db.StreamerProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        
        if (streamer == null)
        {
            // [오시리스의 자비]: 만약 새로운 Channel ID로 검색이 안 된다면, 
            // 혹시 기존에 Slug로만 등록되어 있거나 다른 연동 정보가 있는지 확인하는 로직을 추가할 수 있으나,
            // 현재는 정식 채널 ID 기반의 Greenfield/정형화 원칙에 따라 신규 생성을 우선합니다.
            _logger.LogInformation("📡 [인증] 신규 스트리머 채널 등록 시작 (ChannelId: {ChannelId})", chzzkUid);
            streamer = new StreamerProfile 
            { 
                ChzzkUid = chzzkUid,
                Slug = chzzkUid, 
                IsActive = true,
                IsMasterEnabled = true,
                CreatedAt = KstClock.Now
            };
            _db.StreamerProfiles.Add(streamer);

            _db.SonglistSessions.Add(new SonglistSession 
            { 
                StreamerProfile = streamer, 
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
        _logger.LogInformation($"✅ [인증] 스트리머 프로필 동기화 완료 (ChannelId: {chzzkUid}, Slug: {streamer.Slug})");

        return new AuthResult { IsSuccess = true, ChzzkUid = chzzkUid, ChannelName = channelName, Slug = streamer.Slug };
    }

    public void CleanupRecoveryLock(string chzzkUid) => _botService.CleanupRecoveryLock(chzzkUid);

    public async Task<string> IssueOverlayTokenAsync(string chzzkUid, string role)
    {
        var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (streamer == null) throw new Exception("[오시리스의 거절] 해당 스트리머를 찾을 수 없습니다.");

        // 1. JWT 설정 로드 (전용 검증은 ValidateMandatorySecrets에서 수행됨)
        var secret = _configuration["JwtSettings:Secret"]!;
        
        var issuer = _configuration["JwtSettings:Issuer"] ?? "MooldangBot";
        var audience = _configuration["JwtSettings:Audience"] ?? "MooldangBot_Overlay";
        var expiryDays = int.Parse(_configuration["JwtSettings:ExpiryDays"] ?? "365");

        // 2. 클레임 구성 (역할, UID, 현재 토큰 버전 포함)
        var claims = new[]
        {
            new Claim("StreamerId", chzzkUid.ToLower()),
            new Claim(ClaimTypes.Role, role),
            new Claim("TokenVersion", streamer.OverlayTokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 3. 토큰 서명 및 생성
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddDays(expiryDays);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        _logger.LogInformation($"[오시리스의 공명] 오버레이 토큰 발급 완료 (ChzzkUid: {chzzkUid}, Version: {streamer.OverlayTokenVersion})");
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> RevokeOverlayTokenAsync(string chzzkUid)
    {
        var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (streamer == null) return false;

        // [오시리스의 철퇴]: 버전을 올림으로써 기존 토큰들을 무효화
        streamer.OverlayTokenVersion++;
        await _db.SaveChangesAsync();

        // 🛡️ [오시리스의 기억 소멸]: 캐시 무효화 (불일치 방지)
        string cacheKey = $"{CacheKeyPrefix}{chzzkUid}";
        await _cache.RemoveAsync(cacheKey);

        _logger.LogWarning($"[오시리스의 철퇴] 오버레이 토큰 폐기(버전 업) 및 캐시 무효화 완료 (ChzzkUid: {chzzkUid}, NewVersion: {streamer.OverlayTokenVersion})");
        
        return true;
    }
}
