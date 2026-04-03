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
using MooldangBot.Domain.Common;

namespace MooldangBot.Presentation.Features.Auth
{

    [ApiController]
    public class AuthController(
        IAppDbContext _db, 
        IConfiguration _configuration, 
        IChzzkApiClient _chzzkApi, 
        IChzzkBotService _botService,
        IHttpClientFactory _httpClientFactory,
        IUnifiedCommandService _commandService,
        ILogger<AuthController> _logger) : ControllerBase
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

            // 💡 [핵심]: 네이버 이미지인 경우 고화질 타입으로 강제 변환
            if (url.Contains("pstatic.net"))
            {
                if (url.Contains("type="))
                    url = System.Text.RegularExpressions.Regex.Replace(url, "type=[^&]+", "type=f120_120");
                else
                    url += (url.Contains("?") ? "&" : "?") + "type=f120_120";
            }
        
        try 
        {
            var client = _httpClientFactory.CreateClient();
            // 💡 네이버/치지직 서버가 로봇으로 오해하지 않도록 헤더를 보강합니다.
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Referer", "https://chzzk.naver.com/");
            
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"[ImageProxy] Failed to fetch: {url}, Status: {response.StatusCode}");
                return Results.NotFound();
            }
            
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            var stream = await response.Content.ReadAsStreamAsync();
            
            return Results.Stream(stream, contentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageProxy] Error fetching {url}: {ex.Message}");
            return Results.NotFound();
        }
    }

        [HttpGet("/api/auth/me")]
        public async Task<IResult> GetMyProfile([FromQuery] string? uid, [FromServices] IChzzkApiClient chzzkApi)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new { isAuthenticated = false });
            }

            // [핵심]: uid 파라미터가 있으면 해당 UID를, 없으면 본인 UID를 사용
            var chzzkUid = uid ?? User.FindFirstValue("StreamerId");
            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Results.Json(new { isAuthenticated = true, isChzzkLinked = false });
            }

            // [파로스의 확인]: 실시간 정보 갱신 (API 실패가 로그인을 방해하지 않도록 try-catch 적용)
            try 
            {
                var channelRes = await chzzkApi.GetChannelsAsync(new[] { chzzkUid });
                
                var profile = await _db.StreamerProfiles
                                 .IgnoreQueryFilters() 
                                 .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

                if (profile != null)
                {
                    // 치지직 API 응답이 성공적이었다면 DB 업데이트
                    if (channelRes?.Content?.Data?.FirstOrDefault() is { } channelData)
                    {
                        if (!string.IsNullOrEmpty(channelData.ChannelName)) profile.ChannelName = channelData.ChannelName;
                        if (!string.IsNullOrEmpty(channelData.ChannelImageUrl)) profile.ProfileImageUrl = channelData.ChannelImageUrl;
                        await _db.SaveChangesAsync();
                    }

                    return Results.Json(new {
                        isAuthenticated = true,
                        isChzzkLinked = !string.IsNullOrEmpty(profile.ChzzkAccessToken),
                        channelName = profile.ChannelName ?? "스트리머",
                        profileImageUrl = profile.ProfileImageUrl ?? "",
                        chzzkUid = profile.ChzzkUid
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthMe] Profile refresh failed for {chzzkUid}: {ex.Message}");
                
                // API 실패 시에도 DB에 있는 기존 정보로 로그인 진행
                var profileFallback = await _db.StreamerProfiles
                                .IgnoreQueryFilters() 
                                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                
                if (profileFallback != null)
                {
                    return Results.Json(new {
                        isAuthenticated = true,
                        isChzzkLinked = !string.IsNullOrEmpty(profileFallback.ChzzkAccessToken),
                        channelName = profileFallback.ChannelName ?? "스트리머",
                        profileImageUrl = profileFallback.ProfileImageUrl ?? "",
                        chzzkUid = profileFallback.ChzzkUid
                    });
                }
            }

            return Results.Json(new { isAuthenticated = true, isChzzkLinked = false });
        }

        [HttpGet("/api/auth/logout")]
        public async Task<IResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        }

        [HttpGet("/api/admin/bot/streamers")]
        [Authorize] // 🔐 인증된 사용자라면 일단 목록 조회 허용 (추후 master로 강화 가능)
        public async Task<IResult> GetStreamers([FromServices] IChzzkApiClient chzzkApi)
        {
            // 1. DB에서 모든 스트리머 프로필 조회
            var streamersInDb = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .ToListAsync();

            if (streamersInDb.Any())
            {
                // 2. 20개씩 끊어서 최신 정보 업데이트 (Batch Processing)
                var uids = streamersInDb.Select(s => s.ChzzkUid).ToList();
                for (int i = 0; i < uids.Count; i += 20)
                {
                    var chunk = uids.Skip(i).Take(20).ToList();
                    var channelRes = await chzzkApi.GetChannelsAsync(chunk);

                    if (channelRes?.Content?.Data != null)
                    {
                        foreach (var channelData in channelRes.Content.Data)
                        {
                            var target = streamersInDb.FirstOrDefault(s => s.ChzzkUid == channelData.ChannelId);
                            if (target != null)
                            {
                                // 닉네임과 프로필 이미지 갱신
                                if (!string.IsNullOrEmpty(channelData.ChannelName)) target.ChannelName = channelData.ChannelName;
                                if (!string.IsNullOrEmpty(channelData.ChannelImageUrl)) target.ProfileImageUrl = channelData.ChannelImageUrl;
                            }
                        }
                    }
                }

                // 3. 변경 사항 저장 (하나라도 바뀌었으면 저장)
                await _db.SaveChangesAsync();
            }

            // 4. 최종 결과 반환 (클라이언트로 전달)
            var result = streamersInDb
                .OrderByDescending(p => p.Id)
                .Select(p => new {
                    chzzkUid = p.ChzzkUid,
                    channelName = p.ChannelName,
                    profileImageUrl = p.ProfileImageUrl,
                    isActive = p.IsActive, // [v6.1.6] 활동성 필드로 통합
                    isMasterEnabled = p.IsMasterEnabled, // [v6.1.6] 마스터 스위치 노출
                    lastActiveAt = p.TokenExpiresAt
                })
                .ToList();

            return Results.Json(result);
        }

        [HttpGet("/api/admin/bot/login")]
        public async Task<IResult> BotLogin([FromQuery] string? uid)
        {
            string? clientId = null;
            string redirectUri = $"{BaseDomain}/Auth/callback";

            // [v6.2] 개별 스트리머의 ApiClientId 참조 제거. 항상 시스템 기본값을 사용합니다.

            if (string.IsNullOrEmpty(clientId))
            {
                var clientIdConf = await _db.SystemSettings.FindAsync("ChzzkClientId");
                clientId = clientIdConf?.KeyValue 
                                 ?? _configuration["CHZZK_API:CLIENT_ID"] 
                                 ?? _configuration["ChzzkApi:ClientId"] 
                                 ?? "";
            }

            if (string.IsNullOrEmpty(clientId))
            {
                return Results.Text("[오시리스의 거절]: Chzzk Client ID가 설정되어 있지 않습니다.");
            }

            string state = (string.IsNullOrEmpty(uid) ? "bot_setup_" : $"bot_setup_{uid}_") + Guid.NewGuid().ToString();

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
                // [v6.2] 더 이상 개별 앱 정보를 사용하지 않으므로 커스텀 정보는 null로 고정
                string? customClientId = null;
                string? customClientSecret = null;
                string? customRedirectUrl = null;

                string? targetUid = null;
                
                // UID는 여전히 추출하되, 앱 정보는 시스템 것을 사용함
                if (state != null && state.StartsWith("bot_setup_"))
                {
                    var parts = state.Split('_');
                    if (parts.Length >= 3)
                    {
                        targetUid = parts[2];
                    }
                }

                // 1단계: 토큰 교환 (시스템 전용 앱 정보 사용 - null 전달 시 기본값)
                var tokenRes = await _chzzkApi.ExchangeTokenAsync(code, customClientId, customClientSecret, state, customRedirectUrl);
                if (tokenRes == null || tokenRes.Code != 200 || tokenRes.Content == null) 
                {
                    Console.WriteLine($"[오시리스의 거절]: 토큰 교환 실패 (Result: {tokenRes?.Code ?? 0})");
                    return Results.Text($"[인증 오류] 토큰 발급 실패 (ChzzkApi 반환값 확인 필요)");
                }

                string accessToken = tokenRes.Content.AccessToken ?? "";
                string refreshToken = tokenRes.Content.RefreshToken ?? "";
                int expiresIn = tokenRes.Content.ExpiresIn;
                // [v17.0] 시간대 통일: TokenRenewalService와 동일하게 KST(UTC+9) 기준으로 만료 시각 계산
                var expireDate = KstClock.Now.AddSeconds(expiresIn);

                // 2단계: 봇 설정 흐름인 경우 여기서 처리 후 종료
                if (state != null && state.StartsWith("bot_setup_"))
                {
                    // [추가] 봇 계정 정보 조회 (누가 봇인지 확인)
                    var botMeRes = await _chzzkApi.GetUserMeAsync(accessToken);
                    string setupBotUid = botMeRes?.Content?.EffectiveChannelId ?? "";
                    string setupBotNick = botMeRes?.Content?.EffectiveChannelName ?? "알 수 없음";

                    if (string.IsNullOrEmpty(targetUid))
                    {
                        // 시스템 공용 봇 설정
                        void UpdateOrAddSetting(string key, string value)
                        {
                            var setting = _db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
                            if (setting == null) _db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
                            else setting.KeyValue = value;
                        }

                        UpdateOrAddSetting("BotAccessToken", accessToken);
                        UpdateOrAddSetting("BotRefreshToken", refreshToken);
                        UpdateOrAddSetting("BotTokenExpiresAt", expireDate.Value.ToString("O"));
                        UpdateOrAddSetting("BotUid", setupBotUid);
                        UpdateOrAddSetting("BotNickname", setupBotNick);
                    }
                    else
                    {
                        // [v6.2] 개별 스트리머 전용 봇 설정 필드 삭제로 인해 해당 로직은 더 이상 지원되지 않습니다.
                        _logger.LogWarning($"[오시리스의 거절] {targetUid} 채널의 개별 봇 설정이 시도되었으나 더 이상 지원하지 않는 기능입니다.");
                    }

                    await _db.SaveChangesAsync();

                    string htmlResponse = $@"
                        <!DOCTYPE html>
                        <html lang='ko'>
                        <head><meta charset='UTF-8'><title>봇 연동 성공</title></head>
                        <body style='background-color:#121212; color:#00e676; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; text-align:center;'>
                            <div>
                                <h1 style='color:#0093E9;'>🤖 봇 계정 연동 완료!</h1>
                                <p style='color:#fff;'>[{setupBotNick}] 계정이 물댕봇 전용 봇으로 등록되었습니다.<br>이제 창을 닫아주세요.</p>
                            </div>
                        </body>
                        </html>";

                    return Results.Content(htmlResponse, "text/html; charset=utf-8");
                }

                // 3단계: 일반 사용자(스트리머) 로그인 흐름
                var userMeRes = await _chzzkApi.GetUserMeAsync(accessToken);
                if (userMeRes == null || userMeRes.Code != 200 || userMeRes.Content == null)
                {
                    return Results.Text($"[인증 오류] 사용자 정보 조회 실패");
                }

                // [물멍의 지리]: 시스템 전체의 일관성을 위해 추출된 UID를 소문자로 즉시 정규화하여 대소문자 매칭 오류를 원천 차단합니다.
                string chzzkUid = userMeRes.Content.EffectiveChannelId.ToLower(); 
                string? channelName = userMeRes.Content.EffectiveChannelName;
                string? profileImageUrl = userMeRes.Content.ChannelImageUrl;

                if (string.IsNullOrEmpty(chzzkUid))
                {
                    Console.WriteLine("[오시리스의 거절]: Chzzk UID를 추출하지 못했습니다.");
                    return Results.Text("[인증 오류] 사용자 식별자를 가져올 수 없습니다.");
                }

                var streamer = await _db.StreamerProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                bool isNewStreamer = false;

                if (streamer == null)
                {
                    isNewStreamer = true;
                    streamer = new StreamerProfile 
                    { 
                        ChzzkUid = chzzkUid,
                        IsActive = true,
                        IsMasterEnabled = true, // [v6.1.6] 신규 가입 시 기본 활성화
                    };
                    _db.StreamerProfiles.Add(streamer);

                    _db.SonglistSessions.Add(new SonglistSession 
                    { 
                        StreamerProfile = streamer, 
                        StartedAt = KstClock.Now,
                        IsActive = true 
                    });
                }
                
                if (!string.IsNullOrEmpty(channelName)) streamer.ChannelName = channelName;
                if (!string.IsNullOrEmpty(profileImageUrl)) streamer.ProfileImageUrl = profileImageUrl;

                // [물멍의 지혜]: 토큰을 먼저 심어줘야 시딩(InitializeDefaultCommandsAsync) 과정에서 봇이 정상적으로 초기화되고 명령어를 생성할 수 있습니다.
                streamer.ChzzkAccessToken = accessToken;
                streamer.ChzzkRefreshToken = refreshToken;
                streamer.TokenExpiresAt = expireDate;

                if (isNewStreamer)
                {
                    await _commandService.InitializeDefaultCommandsAsync(chzzkUid);
                }

                await _db.SaveChangesAsync();
                
                // [v16.3.3] 🔐 [봉인 해제]: 수동 로그인을 성공했다는 것은 이미 모든 인증 정보가 신선하다는 증거입니다.
                // 봇 서비스에 박혀있는 '영구 실패 낙인'을 즉시 지우고 즉시 재부팅 가능한 상태로 만듭니다.
                _botService.CleanupRecoveryLock(chzzkUid);
                
                Console.WriteLine($"[파로스의 확인]: DB 저장 완료 및 봇 복구 락 해제 (UID: {chzzkUid})");

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
                    // [v4.7 정규화] 매니저 권한 조회: 전역 시청자 해시 매핑 및 스트리머 프로필 조인
                    var viewerHash = MooldangBot.Application.Common.Security.Sha256Hasher.ComputeHash(chzzkUid);
                    var managedChannels = await _db.StreamerManagers
                        .Include(m => m.StreamerProfile)
                        .Include(m => m.GlobalViewer)
                        .Where(m => m.GlobalViewer!.ViewerUidHash == viewerHash)
                        .Select(m => m.StreamerProfile!.ChzzkUid)
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
