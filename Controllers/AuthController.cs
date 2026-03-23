using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using MooldangAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        
        // 💡 설정(appsettings.json)에서 도메인 정보를 읽어옵니다. 없으면 현재 요청 기반으로 생성합니다.
        private string BaseDomain 
        {
            get {
                var val = _configuration["BaseDomain"];
                if (!string.IsNullOrEmpty(val)) return val;
                
                string scheme = Request.Scheme;
                // 💡 프록시 환경에서 HTTP로 인식되더라도 mooldang.store 도메인이면 HTTPS로 강제 유도
                if (Request.Host.Host.Contains("mooldang.store")) scheme = "https";
                
                return $"{scheme}://{Request.Host}";
            }
        }

        public AuthController(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
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
            using var client = new HttpClient();
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
                var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
                var clientSecretConf = await _db.SystemSettings.FindAsync("ChzzkClientSecret");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                httpClient.Timeout = TimeSpan.FromSeconds(10); // 💡 타임아웃 설정 (10초)

                var tokenRequest = new
                {
                    grantType = "authorization_code",
                    clientId = clientIdConf?.KeyValue,
                    clientSecret = clientSecretConf?.KeyValue,
                    code = code,
                    state = state
                };

                // 1단계: 토큰 교환 시도
                var response = await httpClient.PostAsJsonAsync("https://openapi.chzzk.naver.com/auth/v1/token", tokenRequest);
                if (!response.IsSuccessStatusCode) 
                {
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    return Results.Text($"[인증 오류] 토큰 발급 실패 (Status: {response.StatusCode}): {errorDetail}");
                }

                var tokenContent = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("content");
                string accessToken = tokenContent.GetProperty("accessToken").GetString()!;
                string refreshToken = tokenContent.GetProperty("refreshToken").GetString()!;
                int expiresIn = tokenContent.GetProperty("expiresIn").GetInt32();

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

                // 2단계: 사용자 정보 조회
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var profileResponse = await httpClient.GetAsync("https://openapi.chzzk.naver.com/open/v1/users/me");
                if (!profileResponse.IsSuccessStatusCode)
                {
                    return Results.Text($"[인증 오류] 사용자 정보 조회 실패 (Status: {profileResponse.StatusCode})");
                }
                
                var profileRes = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
                string chzzkUid = profileRes.GetProperty("content").GetProperty("channelId").GetString()!;
                
                string? channelName = null;
                string? profileImageUrl = null;
                if (profileRes.GetProperty("content").TryGetProperty("channelName", out var nameEl)) channelName = nameEl.GetString();
                if (profileRes.GetProperty("content").TryGetProperty("channelImageUrl", out var imgEl)) profileImageUrl = imgEl.GetString();

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

                // 🔐 세션 쿠키 생성 (치지직 UID를 StreamerId 클레임으로 저장)
                var claims = new List<Claim>
                {
                    new Claim("StreamerId", chzzkUid),
                    new Claim(ClaimTypes.Name, channelName ?? "Streamer")
                };

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
