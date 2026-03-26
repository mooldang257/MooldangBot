using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Presentation.Features.Auth
{

    [ApiController]
    public class AuthController(
        IAppDbContext _db, 
        IConfiguration _configuration, 
        IChzzkApiClient _chzzkApi, 
        IHttpClientFactory _httpClientFactory) : ControllerBase
    {
        // 🔐 [Zero-Git] 설정(appsettings.json 또는 .env)에서 도메인 정보를 읽어옵니다. 
// 다중 인스턴스 환경에서 리다이렉트 및 쿠키 도메인 정합성을 위해 필수값으로 취급합니다.
private string BaseDomain 
{
    get {
        var val = _configuration["BASE_DOMAIN"];
        if (!string.IsNullOrEmpty(val)) return val;
        
        throw new Exception("환경 설정 파일(.env 등)에 'BASE_DOMAIN'이 설정되어 있지 않습니다. 멀티 인스턴스 운영을 위해 필수 항목입니다.");
    }
}

        [HttpGet("/api/auth/chzzk-login")]
        public async Task<IResult> ChzzkLogin()
        {
            var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
            string clientId = clientIdConf?.KeyValue ?? "";
            string redirectUri = $"{BaseDomain}/Auth/callback";

            string state = Guid.NewGuid().ToString();

            string encodedRedirect = System.Net.WebUtility.UrlEncode(redirectUri);
            string authUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={encodedRedirect}&state={state}";
            return Results.Redirect(authUrl);
        }

        [HttpGet("/api/proxy/image")]
    public async Task<IResult> ProxyImage([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url)) return Results.NotFound();
        
        try 
        {
            var client = _httpClientFactory.CreateClient();
            // 💡 네이버/치지직 서버가 로봇으로 오해하지 않도록 User-Agent를 추가합니다.
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return Results.NotFound();
            
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            var stream = await response.Content.ReadAsStreamAsync();
            
            return Results.Stream(stream, contentType);
        }
        catch 
        {
            return Results.NotFound();
        }
    }

        [HttpGet("/api/auth/me")]
        public async Task<IResult> GetMyProfile()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new { isAuthenticated = false });
            }

            var chzzkUid = User.FindFirstValue("StreamerId");
            var profile = await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile != null && !string.IsNullOrEmpty(profile.ChzzkAccessToken))
            {
                return Results.Json(new {
                    isAuthenticated = true,
                    isChzzkLinked = true,
                    channelName = profile.ChannelName ?? "스트리머",
                    profileImageUrl = profile.ProfileImageUrl ?? "",
                    chzzkUid = profile.ChzzkUid
                });
            }

            // 치지직 토큰이 없으면 연동이 안 된 것으로 간주
            return Results.Json(new { isAuthenticated = true, isChzzkLinked = false });
        }

        [HttpGet("/api/admin/bot/login")]
        public async Task<IResult> BotLogin()
        {
            var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
            string clientId = clientIdConf?.KeyValue ?? "";

            string redirectUri = $"{BaseDomain}/Auth/callback";
            string state = "bot_setup_" + Guid.NewGuid().ToString();

            string encodedRedirect = System.Net.WebUtility.UrlEncode(redirectUri);
            string authUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={encodedRedirect}&state={state}";
            return Results.Redirect(authUrl);
        }

        [HttpGet("/Auth/callback")]
        [AllowAnonymous]
        public async Task<IResult> AuthCallback([FromQuery] string? code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code)) return Results.Text("인증 코드가 없습니다.");

            try
            {
                // 1단계: 토큰 교환 시도
                var tokenRes = await _chzzkApi.ExchangeTokenAsync(code, state);
                if (tokenRes == null || tokenRes.Code != 200 || tokenRes.Content == null) 
                {
                    return Results.Text($"[인증 오류] 토큰 발급 실패 (ChzzkApi 반환값 확인 필요)");
                }

                string accessToken = tokenRes.Content.AccessToken;
                string refreshToken = tokenRes.Content.RefreshToken;
                int expiresIn = tokenRes.Content.ExpiresIn;

                if (state != null && state.StartsWith("bot_setup_"))
                {
                    DateTime expireDate = DateTime.Now.AddSeconds(expiresIn);

                    void UpdateOrAddSetting(string key, string value)
                    {
                        var setting = _db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
                        if (setting == null) _db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
                        else setting.KeyValue = value;
                    }

                    UpdateOrAddSetting("BotAccessToken", accessToken);
                    UpdateOrAddSetting("BotRefreshToken", refreshToken);
                    UpdateOrAddSetting("BotTokenExpiresAt", expireDate.ToString("O"));

                    await _db.SaveChangesAsync();

                    string htmlResponse = @"
                        <!DOCTYPE html>
                        <html lang='ko'>
                        <head><meta charset='UTF-8'><title>봇 연동 성공</title></head>
                        <body style='background-color:#121212; color:#00e676; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; text-align:center;'>
                            <div>
                                <h1>🎉 시스템 봇 연동 완료!</h1>
                                <p style='color:#fff;'>물댕봇 전용 토큰이 DB(SystemSettings)에 안전하게 저장되었습니다.<br>이제 창을 닫아주세요.</p>
                            </div>
                        </body>
                        </html>";

                    return Results.Content(htmlResponse, "text/html; charset=utf-8");
                }

                // 2단계: 사용자 정보 조회 (ChzzkApiClient 사용)
                var userMeRes = await _chzzkApi.GetUserMeAsync(accessToken);
                if (userMeRes == null || userMeRes.Code != 200 || userMeRes.Content == null)
                {
                    return Results.Text($"[인증 오류] 사용자 정보 조회 실패");
                }
                
                string chzzkUid = userMeRes.Content.ChannelId;
                string? channelName = userMeRes.Content.ChannelName;
                string? profileImageUrl = userMeRes.Content.ChannelImageUrl;

                var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

                if (streamer == null)
                {
                    streamer = new StreamerProfile { ChzzkUid = chzzkUid };
                    _db.StreamerProfiles.Add(streamer);
                }
                
                if (!string.IsNullOrEmpty(channelName)) streamer.ChannelName = channelName;
                if (!string.IsNullOrEmpty(profileImageUrl)) streamer.ProfileImageUrl = profileImageUrl;

                streamer.ChzzkAccessToken = accessToken;
                streamer.ChzzkRefreshToken = refreshToken;
                streamer.TokenExpiresAt = DateTime.Now.AddSeconds(expiresIn);

                await _db.SaveChangesAsync();

                // 3단계: 역할 및 권한 조회 (RBAC)
                var userRole = "viewer";
                var allowedChannels = new List<string> { chzzkUid }; // 본인 채널은 기본 포함

                // 마스터 확인 (환경 설정 .env 기반)
                string masterUid = _configuration["MASTER_UID"] ?? "";
                string botUid = _configuration["BOT_UID"] ?? "";

                if (!string.IsNullOrEmpty(masterUid) && chzzkUid == masterUid || 
                    !string.IsNullOrEmpty(botUid) && chzzkUid == botUid)
                {
                    userRole = "master";
                }
                else
                {
                    // 매니저 권한 조회
                    var managedChannels = await _db.StreamerManagers
                        .Where(m => m.ManagerChzzkUid == chzzkUid)
                        .Select(m => m.StreamerChzzkUid)
                        .ToListAsync();

                    if (managedChannels.Any())
                    {
                        userRole = "manager";
                        allowedChannels.AddRange(managedChannels);
                    }
                    else if (streamer != null)
                    {
                        userRole = "streamer";
                    }
                }

                // 🔐 세션 쿠키 생성
                var claims = new List<Claim>
                {
                    new Claim("StreamerId", chzzkUid),
                    new Claim(ClaimTypes.Name, channelName ?? "User"),
                    new Claim(ClaimTypes.Role, userRole)
                };

                // 관리 권한이 있는 모든 채널 ID 추가
                foreach (var channelId in allowedChannels.Distinct())
                {
                    claims.Add(new Claim("AllowedChannelId", channelId));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return Results.Redirect("/");
            }
            catch (Exception ex) 
            { 
                string errorMsg = ex.InnerException != null ? $"{ex.Message} --> {ex.InnerException.Message}" : ex.Message;
                return Results.Text($"에러 발생: {errorMsg}"); 
            }
        }
    }
}
