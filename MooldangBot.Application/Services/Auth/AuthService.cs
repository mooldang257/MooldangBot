using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Security;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace MooldangBot.Application.Services.Auth;

/// <summary>
/// [오시리스의 전령]: 사용자 인증 및 권한 관리의 핵심 비즈니스 로직을 담당합니다.
/// </summary>
public class AuthService(
    IAppDbContext _db,
    IChzzkApiClient _chzzkApi,
    IConfiguration _configuration,
    IChzzkBotService _botService,
    IUnifiedCommandService _commandService,
    ILogger<AuthService> _logger) : IAuthService
{
    private string BaseDomain => _configuration["BASE_DOMAIN"] 
        ?? throw new Exception("BASE_DOMAIN이 설정되어 있지 않습니다.");

    public async Task<AuthMetadata> GenerateAuthMetadataAsync(string? targetUid = null)
    {
        // 1. 시스템 ClientId 조회
        var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
        string clientId = clientIdConf?.KeyValue 
                         ?? _configuration["CHZZK_API:CLIENT_ID"] 
                         ?? _configuration["ChzzkApi:ClientId"] 
                         ?? throw new Exception("Chzzk Client ID가 설정되어 있지 않습니다.");

        // 2. PKCE & State 생성
        string state = Guid.NewGuid().ToString("N");
        string verifier = CryptoHelper.GenerateCodeVerifier();
        string challenge = CryptoHelper.GenerateCodeChallenge(verifier);

        // 3. 인증 URL 구성
        string redirectUri = $"{BaseDomain}/Auth/callback";
        string encodedRedirect = System.Net.WebUtility.UrlEncode(redirectUri);
        
        // 치지직 API PKCE 파라미터 규격 적용 (예상 파라미터: code_challenge, code_challenge_method)
        string authUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={encodedRedirect}&state={state}&code_challenge={challenge}&code_challenge_method=S256";

        _logger.LogInformation($"[오시리스의 전령] 신규 인증 요청 생성 (State: {state}, TargetUid: {targetUid})");
        
        return new AuthMetadata(authUrl, state, verifier);
    }

    public async Task<AuthResult> ProcessCallbackAsync(string code, AuthSessionData cachedData)
    {
        try
        {
            // 1. 토큰 교환 (PKCE Verifier 포함)
            var tokenRes = await _chzzkApi.ExchangeTokenAsync(code, null, null, cachedData.State, null, cachedData.CodeVerifier);
            
            if (tokenRes?.Code != 200 || tokenRes?.Content == null)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "토큰 교환 실패" };
            }

            var content = tokenRes.Content;
            var expireDate = KstClock.Now.AddSeconds(content.ExpiresIn);

            // 2. 봇 설정 흐름 처리
            if (cachedData.State.StartsWith("bot_setup_") || !string.IsNullOrEmpty(cachedData.TargetUid))
            {
                return await HandleBotSetupAsync(content.AccessToken, content.RefreshToken, expireDate, cachedData.TargetUid);
            }

            // 3. 일반 스트리머 로그인 흐름 처리
            var userMeRes = await _chzzkApi.GetUserMeAsync(content.AccessToken);
            if (userMeRes?.Code != 200 || userMeRes?.Content == null)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "사용자 정보 조회 실패" };
            }

            string chzzkUid = userMeRes.Content.EffectiveChannelId.ToLower();
            string channelName = userMeRes.Content.EffectiveChannelName ?? "알 수 없음";

            var result = await SyncStreamerProfileAsync(chzzkUid, channelName, userMeRes.Content.ChannelImageUrl, content.AccessToken, content.RefreshToken, expireDate);
            
            // [v17.0] 봇 서비스 복구 락 해제 및 명령어 초기화 동기화
            _botService.CleanupRecoveryLock(chzzkUid);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[오시리스의 실책] 인증 콜백 처리 중 오류 발생");
            return new AuthResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<AuthResult> HandleBotSetupAsync(string accessToken, string refreshToken, KstClock? expireDate, string? targetUid)
    {
        var botMeRes = await _chzzkApi.GetUserMeAsync(accessToken);
        string setupBotUid = botMeRes?.Content?.EffectiveChannelId ?? "";
        string setupBotNick = botMeRes?.Content?.EffectiveChannelName ?? "알 수 없음";

        // 시스템 공용 봇 설정 업데이트
        void UpdateOrAddSetting(string key, string value)
        {
            var setting = _db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
            if (setting == null) _db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
            else setting.KeyValue = value;
        }

        UpdateOrAddSetting("BotAccessToken", accessToken);
        UpdateOrAddSetting("BotRefreshToken", refreshToken);
        UpdateOrAddSetting("BotTokenExpiresAt", expireDate?.Value.ToString("O") ?? "");
        UpdateOrAddSetting("BotUid", setupBotUid);
        UpdateOrAddSetting("BotNickname", setupBotNick);

        await _db.SaveChangesAsync();

        return new AuthResult { IsSuccess = true, ChannelName = setupBotNick, RedirectUrl = "/bot-setup-success" };
    }

    private async Task<AuthResult> SyncStreamerProfileAsync(string chzzkUid, string channelName, string? profileImageUrl, string accessToken, string refreshToken, KstClock? expireDate)
    {
        var streamer = await _db.StreamerProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        bool isNewStreamer = false;

        if (streamer == null)
        {
            isNewStreamer = true;
            streamer = new StreamerProfile 
            { 
                ChzzkUid = chzzkUid,
                IsActive = true,
                IsMasterEnabled = true,
            };
            _db.StreamerProfiles.Add(streamer);

            _db.SonglistSessions.Add(new SonglistSession 
            { 
                StreamerProfile = streamer, 
                StartedAt = KstClock.Now,
                IsActive = true 
            });
        }

        streamer.ChannelName = channelName;
        streamer.ProfileImageUrl = profileImageUrl;
        streamer.ChzzkAccessToken = accessToken;
        streamer.ChzzkRefreshToken = refreshToken;
        streamer.TokenExpiresAt = expireDate;

        if (isNewStreamer)
        {
            await _commandService.InitializeDefaultCommandsAsync(chzzkUid);
        }

        await _db.SaveChangesAsync(); // [물멍의 제언]: 원자적 저장

        return new AuthResult { IsSuccess = true, ChzzkUid = chzzkUid, ChannelName = channelName };
    }

    public void CleanupRecoveryLock(string chzzkUid) => _botService.CleanupRecoveryLock(chzzkUid);

    public async Task<string> IssueOverlayTokenAsync(string chzzkUid, string role)
    {
        var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (streamer == null) throw new Exception("[오시리스의 거절] 해당 스트리머를 찾을 수 없습니다.");

        // 1. JWT 설정 로드
        var secret = _configuration["JwtSettings:Secret"] ?? "Default_Secure_Key_for_MooldangBot_Resonance";
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

        _logger.LogWarning($"[오시리스의 철퇴] 오버레이 토큰 폐기(버전 업) 완료 (ChzzkUid: {chzzkUid}, NewVersion: {streamer.OverlayTokenVersion})");
        
        return true;
    }
}
