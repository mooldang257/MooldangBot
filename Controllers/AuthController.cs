using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using MooldangAPI.Models;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        
        // 💡 설정(appsettings.json)에서 도메인 정보를 읽어옵니다.
        private string BaseDomain => _configuration["BaseDomain"] ?? "https://www.mooldang.store";

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

            var naverId = User.FindFirstValue("StreamerId") ?? "";
            string state = $"{Guid.NewGuid()}_naver_{naverId}";

            string authUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={redirectUri}&state={state}";
            return Results.Redirect(authUrl);
        }

        [HttpGet("/api/auth/me")]
        public async Task<IResult> GetMyProfile()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new { isAuthenticated = false });
            }

            var naverId = User.FindFirstValue("StreamerId");
            // 중복 레코드가 혹시 있다면 ChzzkUid가 존재하는 것을 우선 찾습니다.
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId && p.ChzzkUid != null) 
                          ?? await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

            if (profile != null && !string.IsNullOrEmpty(profile.ChzzkUid) && !string.IsNullOrEmpty(profile.ChzzkAccessToken))
            {
                return Results.Json(new {
                    isAuthenticated = true,
                    isChzzkLinked = true,
                    channelName = profile.ChannelName ?? "스트리머",
                    profileImageUrl = profile.ProfileImageUrl ?? "",
                    chzzkUid = profile.ChzzkUid
                });
            }

            return Results.Json(new { isAuthenticated = true, isChzzkLinked = false });
        }

        [HttpGet("/api/admin/bot/login")]
        public async Task<IResult> BotLogin()
        {
            var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
            string clientId = clientIdConf?.KeyValue ?? "";

            string redirectUri = $"{BaseDomain}/Auth/callback";
            string state = "bot_setup_" + Guid.NewGuid().ToString();

            string authUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={redirectUri}&state={state}";
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

                var tokenRequest = new
                {
                    grantType = "authorization_code",
                    clientId = clientIdConf?.KeyValue,
                    clientSecret = clientSecretConf?.KeyValue,
                    code = code,
                    state = state
                };

                var response = await httpClient.PostAsJsonAsync("https://openapi.chzzk.naver.com/auth/v1/token", tokenRequest);
                if (!response.IsSuccessStatusCode) return Results.Text($"토큰 발급 실패: {await response.Content.ReadAsStringAsync()}");

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

                // 일반 스트리머 로그인 (State에서 naverId 복구)
                string? callbackNaverId = null;
                if (state != null && state.Contains("_naver_"))
                {
                    callbackNaverId = state.Split("_naver_").LastOrDefault();
                }
                if (string.IsNullOrEmpty(callbackNaverId)) callbackNaverId = User.FindFirstValue("StreamerId"); // fallback

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var profileRes = await httpClient.GetFromJsonAsync<JsonElement>("https://openapi.chzzk.naver.com/open/v1/users/me");
                string chzzkUid = profileRes.GetProperty("content").GetProperty("channelId").GetString()!;
                
                string? channelName = null;
                string? profileImageUrl = null;
                if (profileRes.GetProperty("content").TryGetProperty("channelName", out var nameEl)) channelName = nameEl.GetString();
                if (profileRes.GetProperty("content").TryGetProperty("channelImageUrl", out var imgEl)) profileImageUrl = imgEl.GetString();

                var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid) 
                            ?? await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == callbackNaverId && !string.IsNullOrEmpty(p.NaverId));

                if (streamer == null)
                {
                    streamer = new StreamerProfile { ChzzkUid = chzzkUid, NaverId = callbackNaverId ?? "" };
                    _db.StreamerProfiles.Add(streamer);
                }
                else if (!string.IsNullOrEmpty(callbackNaverId))
                {
                    // Update NaverId just in case it was blank
                    streamer.NaverId = callbackNaverId;
                    streamer.ChzzkUid = chzzkUid;
                }
                
                if (!string.IsNullOrEmpty(channelName)) streamer.ChannelName = channelName;
                if (!string.IsNullOrEmpty(profileImageUrl)) streamer.ProfileImageUrl = profileImageUrl;

                streamer.ChzzkAccessToken = accessToken;
                streamer.ChzzkRefreshToken = refreshToken;
                streamer.TokenExpiresAt = DateTime.Now.AddSeconds(expiresIn);

                await _db.SaveChangesAsync();
                return Results.Redirect("/");
            }
            catch (Exception ex) { return Results.Text($"에러 발생: {ex.Message}"); }
        }
    }
}
