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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;

namespace MooldangBot.Presentation.Features.Auth
{

    [ApiController]
    public class AuthController(
        IAppDbContext _db, 
        IConfiguration _configuration, 
        IChzzkApiClient _chzzkApi, 
        IAuthService _authService,
        Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache,
        IHttpClientFactory _httpClientFactory,
        ILogger<AuthController> _logger) : ControllerBase
    {
        private const string StateCookieName = "__Mooldang_Auth_State";
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
            try 
            {
                // [오시리스의 전령]: 메타데이터 생성 (PKCE Verifier 포함)
                var metadata = await _authService.GenerateAuthMetadataAsync();
                
                // [물멍의 제언]: Double-Submit Cookie 패턴 적용
                Response.Cookies.Append(StateCookieName, metadata.State, new CookieOptions 
                { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Lax, 
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
                });

                // [분산 캐시]: 세션 데이터 보관 (Verifier 등)
                var sessionData = new AuthSessionData { State = metadata.State, CodeVerifier = metadata.CodeVerifier };
                var json = JsonSerializer.Serialize(sessionData);
                await _cache.SetStringAsync($"auth:state:{metadata.State}", json, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                });

                return Results.Redirect(metadata.AuthUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[오시리스의 거절] 로그인 URL 생성 실패");
                return Results.Text($"[인증 오류] {ex.Message}");
            }
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
            try 
            {
                var metadata = await _authService.GenerateAuthMetadataAsync(uid);
                
                // Double-Submit Cookie
                Response.Cookies.Append(StateCookieName, metadata.State, new CookieOptions 
                { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Lax, 
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
                });

                var sessionData = new AuthSessionData { State = metadata.State, CodeVerifier = metadata.CodeVerifier, TargetUid = uid };
                await _cache.SetStringAsync($"auth:state:{metadata.State}", JsonSerializer.Serialize(sessionData), new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                });

                return Results.Redirect(metadata.AuthUrl);
            }
            catch (Exception ex)
            {
                return Results.Text($"[봇 인증 오류] {ex.Message}");
            }
        }

        [HttpGet("/Auth/callback")]
        [AllowAnonymous]
        public async Task<IResult> AuthCallback([FromQuery] string? code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state)) 
                return Results.Text("[오시리스의 거절] 필수 인증 파라미터가 누락되었습니다.");

            // 🛡️ [물멍의 수호]: Double-Submit Cookie 검증
            var stateFromCookie = Request.Cookies[StateCookieName];
            if (string.IsNullOrEmpty(stateFromCookie) || stateFromCookie != state)
            {
                _logger.LogWarning($"[CSRF 감지] State 불일치 (URL: {state}, Cookie: {stateFromCookie})");
                return Results.Text("[보안 경고] 인증 세션이 유효하지 않거나 변조되었습니다. 다시 시도해 주세요.");
            }

            // [분산 캐시]: 세션 데이터 조회
            var cachedJson = await _cache.GetStringAsync($"auth:state:{state}");
            if (string.IsNullOrEmpty(cachedJson))
            {
                return Results.Text("[인증 만료] 인증 시간이 초과되었습니다. 다시 로그인해 주세요.");
            }

            var cachedData = JsonSerializer.Deserialize<AuthSessionData>(cachedJson);
            if (cachedData == null) return Results.Text("[시스템 오류] 인증 세션 데이터가 손상되었습니다.");

            // 🚀 [AuthService]: 핵심 비즈니스 로직(토큰 교환, DB 동기화 등) 처리
            var result = await _authService.ProcessCallbackAsync(code, cachedData);

            if (!result.IsSuccess)
            {
                return Results.Text($"[인증 실패] {result.ErrorMessage}");
            }

            // 봇 설정인 경우 특수 결과 반환
            if (!string.IsNullOrEmpty(result.RedirectUrl))
            {
                string htmlResponse = $@"
                    <!DOCTYPE html>
                    <html lang='ko'>
                    <head><meta charset='UTF-8'><title>봇 연동 성공</title></head>
                    <body style='background-color:#121212; color:#00e676; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; text-align:center;'>
                        <div>
                            <h1 style='color:#0093E9;'>🤖 봇 계정 연동 완료!</h1>
                            <p style='color:#fff;'>[{result.ChannelName}] 계정이 물댕봇 전용 봇으로 등록되었습니다.<br>이제 창을 닫아주세요.</p>
                        </div>
                    </body>
                    </html>";
                return Results.Content(htmlResponse, "text/html; charset=utf-8");
            }

            // 일반 로그인: 역할(RBAC) 조회 및 클레임 생성
            string chzzkUid = result.ChzzkUid!;
            string channelName = result.ChannelName!;
            
            var userRole = "viewer";
            var allowedChannels = new List<string> { chzzkUid };

            string masterUid = _configuration["MASTER_UID"] ?? "";
            string botUid = _configuration["BOT_UID"] ?? "";

            if ((!string.IsNullOrEmpty(masterUid) && chzzkUid == masterUid) || 
                (!string.IsNullOrEmpty(botUid) && chzzkUid == botUid))
            {
                userRole = "master";
            }
            else
            {
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
                else
                {
                    userRole = "streamer";
                }
            }

            // 🔐 세션 쿠키 생성
            var claims = new List<Claim>
            {
                new Claim("StreamerId", chzzkUid),
                new Claim(ClaimTypes.Name, channelName),
                new Claim(ClaimTypes.Role, userRole)
            };

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

            // 사용한 쿠키 및 캐시 삭제
            Response.Cookies.Delete(StateCookieName);
            await _cache.RemoveAsync($"auth:state:{state}");

            return Results.Redirect("/");
        }
    }
}
