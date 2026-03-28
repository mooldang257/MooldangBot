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
        /// <summary>
        /// [파로스의 자각]: 설정(appsettings.json 또는 .env)에서 도메인 정보를 읽어옵니다. 
        /// 다중 인스턴스 환경에서 리다이렉트 및 쿠키 도메인 정합성을 위해 필수값으로 취급합니다.
        /// </summary>
        private string BaseDomain 
        {
            get {
                var val = _configuration["BASE_DOMAIN"];
                if (!string.IsNullOrEmpty(val)) return val;
                
                throw new Exception("[오시리스의 거절]: 환경 설정 파일(.env 등)에 'BASE_DOMAIN' 혹은 'DEV_BASE_DOMAIN'이 설정되어 있지 않습니다.");
            }
        }

        [HttpGet("/api/auth/chzzk-login")]
        public async Task<IResult> ChzzkLogin()
        {
            // [텔로스5의 순환]: DB 설정을 우선하되 환경 변수를 안정적인 폴백으로 사용
            var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
            string clientId = clientIdConf?.KeyValue 
                             ?? _configuration["CHZZK_API:CLIENT_ID"] 
                             ?? _configuration["ChzzkApi:ClientId"] 
                             ?? "";
                             
            if (string.IsNullOrEmpty(clientId))
            {
                return Results.Text("[오시리스의 거절]: Chzzk Client ID가 설정되어 있지 않습니다.");
            }

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
            Console.WriteLine($"[오시리스의 확인]: /api/auth/me 호출 (Claim UID: {chzzkUid ?? "NULL"})");
            
            // [파로스의 확인]: 전역 쿼리 필터의 간섭을 방지하기 위해 IgnoreQueryFilters()를 명시적으로 사용합니다.
            var profile = await _db.StreamerProfiles.AsNoTracking()
                             .IgnoreQueryFilters() 
                             .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile != null && !string.IsNullOrEmpty(profile.ChzzkAccessToken))
            {
                Console.WriteLine($"[파로스의 확인]: 연동된 프로필 발견 - {profile.ChannelName} ({chzzkUid})");
                return Results.Json(new {
                    isAuthenticated = true,
                    isChzzkLinked = true,
                    channelName = profile.ChannelName ?? "스트리머",
                    profileImageUrl = profile.ProfileImageUrl ?? "",
                    chzzkUid = profile.ChzzkUid
                });
            }

            Console.WriteLine($"[오시리스의 경고]: 프로필을 찾을 수 없거나 토큰이 없습니다. (UID: {chzzkUid})");
            // 치지직 토큰이 없으면 연동이 안 된 것으로 간주
            return Results.Json(new { isAuthenticated = true, isChzzkLinked = false });
        }

        [HttpGet("/api/admin/bot/login")]
        public async Task<IResult> BotLogin()
        {
            // [텔로스5의 순환]: 봇 연동 시에도 환경 변수를 폴백으로 활용
            var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
            string clientId = clientIdConf?.KeyValue 
                             ?? _configuration["CHZZK_API:CLIENT_ID"] 
                             ?? _configuration["ChzzkApi:ClientId"] 
                             ?? "";

            if (string.IsNullOrEmpty(clientId))
            {
                return Results.Text("[오시리스의 거절]: Chzzk Client ID가 설정되어 있지 않습니다.");
            }

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
            Console.WriteLine($"[파로스의 확인]: Auth Callback 시작 (Code: {code.Substring(0, 5)}..., State: {state})");

            try
            {
                // 1단계: 토큰 교환 시도
                var tokenRes = await _chzzkApi.ExchangeTokenAsync(code, state);
                if (tokenRes == null || tokenRes.Code != 200 || tokenRes.Content == null) 
                {
                    Console.WriteLine($"[오시리스의 거절]: 토큰 교환 실패 (Result: {tokenRes?.Code ?? 0})");
                    return Results.Text($"[인증 오류] 토큰 발급 실패 (ChzzkApi 반환값 확인 필요)");
                }

                string accessToken = tokenRes.Content.AccessToken;
                string refreshToken = tokenRes.Content.RefreshToken;
                int expiresIn = tokenRes.Content.ExpiresIn;
                Console.WriteLine("[파로스의 확인]: 토큰 교환 성공");

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
                
                string chzzkUid = userMeRes.Content.EffectiveChannelId;
                string? channelName = userMeRes.Content.EffectiveChannelName;
                string? profileImageUrl = userMeRes.Content.ChannelImageUrl;

                if (string.IsNullOrEmpty(chzzkUid))
                {
                    Console.WriteLine("[오시리스의 거절]: Chzzk UID를 추출하지 못했습니다. (JSON 매핑 오류 또는 API 응답 이상)");
                    return Results.Text("[인증 오류] 사용자 식별자를 가져올 수 없습니다.");
                }

                var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

                if (streamer == null)
                {
                    streamer = new StreamerProfile 
                    { 
                        ChzzkUid = chzzkUid,
                        IsBotEnabled = true,
                        IsOmakaseEnabled = true,
                        SongCommand = "!신청",
                        SongPrice = 1000,
                        OmakaseCommand = "!물마카세",
                        OmakasePrice = 10000 
                    };
                    _db.StreamerProfiles.Add(streamer);

                    // [추가] 신규 가입 시 노래 신청 세션 자동 시작 (기본 활성화)
                    _db.SonglistSessions.Add(new SonglistSession 
                    { 
                        ChzzkUid = chzzkUid, 
                        StartedAt = DateTime.Now, 
                        IsActive = true 
                    });

                    // [오시리스의 정렬]: 신규 가입 시 필수 통합 명령어(UnifiedCommands) 5종 자동 탑재
                    _db.UnifiedCommands.AddRange(
                        new UnifiedCommand 
                        { 
                            ChzzkUid = chzzkUid, Keyword = "!신청", Category = CommandCategory.Feature, 
                            CostType = CommandCostType.Cheese, Cost = 1000, FeatureType = "SongRequest", 
                            RequiredRole = CommandRole.Viewer, IsActive = true, UpdatedAt = DateTime.Now 
                        },
                        new UnifiedCommand 
                        { 
                            ChzzkUid = chzzkUid, Keyword = "!송리스트", Category = CommandCategory.System, 
                            CostType = CommandCostType.None, Cost = 0, FeatureType = "SonglistToggle", 
                            ResponseText = "송리스트가 {송리스트상태}되었습니다. ✨", 
                            RequiredRole = CommandRole.Manager, IsActive = true, UpdatedAt = DateTime.Now 
                        },
                        new UnifiedCommand
                        {
                            ChzzkUid = chzzkUid, Keyword = "!공지", Category = CommandCategory.System,
                            CostType = CommandCostType.None, Cost = 0, FeatureType = "Notice", 
                            ResponseText = "공지사항: {내용}", 
                            RequiredRole = CommandRole.Manager, IsActive = true, UpdatedAt = DateTime.Now
                        },
                        new UnifiedCommand
                        {
                            ChzzkUid = chzzkUid, Keyword = "!방제", Category = CommandCategory.System,
                            CostType = CommandCostType.None, Cost = 0, FeatureType = "Title", 
                            ResponseText = "방송 제목이 변경되었습니다: {내용}", 
                            RequiredRole = CommandRole.Manager, IsActive = true, UpdatedAt = DateTime.Now
                        },
                        new UnifiedCommand
                        {
                            ChzzkUid = chzzkUid, Keyword = "!카테고리", Category = CommandCategory.System,
                            CostType = CommandCostType.None, Cost = 0, FeatureType = "StreamCategory", 
                            ResponseText = "카테고리가 변경되었습니다: {내용}", 
                            RequiredRole = CommandRole.Manager, IsActive = true, UpdatedAt = DateTime.Now
                        }
                    );
                }
                
                if (!string.IsNullOrEmpty(channelName)) streamer.ChannelName = channelName;
                if (!string.IsNullOrEmpty(profileImageUrl)) streamer.ProfileImageUrl = profileImageUrl;

                streamer.ChzzkAccessToken = accessToken;
                streamer.ChzzkRefreshToken = refreshToken;
                streamer.TokenExpiresAt = DateTime.Now.AddSeconds(expiresIn);

                Console.WriteLine($"[파로스의 확인]: 프로필 정보 업데이트 중 (UID: {chzzkUid})");
                await _db.SaveChangesAsync();
                Console.WriteLine("[파로스의 확인]: DB 저장 완료");

                // 3단계: 역할 및 권한 조회 (RBAC)
                var userRole = "viewer";
                var allowedChannels = new List<string> { chzzkUid }; // 본인 채널은 기본 포함

                // [파로스의 증명]: 마스터 및 봇의 UID를 확인하여 절대 권한을 부여합니다.
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
