using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
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
using MooldangBot.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.RateLimiting;

namespace MooldangBot.Presentation.Features.Auth
{

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}")] // 새로운 버전 명시 경로
    [Route("api")]                       // 레거시 하위 호환 경로
    public class AuthController(
        IAppDbContext _db, 
        IConfiguration _configuration, 
        IChzzkApiClient _chzzkApi, 
        IAuthService _authService,
        IIdentityCacheService _identityCache,
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
        [HttpGet("/api/v1/auth/chzzk-login")]
        [EnableRateLimiting("strict-auth")]
        public async Task<IActionResult> ChzzkLogin([FromQuery] string? type)
        {
            try 
            {
                var loginType = type ?? "streamer";
                // [오시리스의 전령]: 메타데이터 생성 (PKCE Verifier 포함)
                var metadata = await _authService.GenerateAuthMetadataAsync(null, loginType);
                
                // [물멍의 제언]: Double-Submit Cookie 패턴 적용
                Response.Cookies.Append(StateCookieName, metadata.State, new CookieOptions 
                { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Lax, 
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
                });

                // [분산 캐시]: 세션 데이터 보관 (Verifier 및 로그인 타입 포함)
                var sessionData = new AuthSessionData { 
                    State = metadata.State, 
                    CodeVerifier = metadata.CodeVerifier,
                    LoginType = loginType
                };
                var json = JsonSerializer.Serialize(sessionData);
                await _cache.SetStringAsync($"auth:state:{metadata.State}", json, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                });

                return Redirect(metadata.AuthUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[오시리스의 거절] 로그인 URL 생성 실패");
                return Content($"[인증 오류] {ex.Message}");
            }
        }

        [HttpGet("proxy/image")]
        public async Task<IActionResult> ProxyImage([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url)) return NotFound();

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
                return NotFound();
            }
            
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            var stream = await response.Content.ReadAsStreamAsync();
            
            return File(stream, contentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageProxy] Error fetching {url}: {ex.Message}");
            return NotFound();
        }
    }

        [HttpGet("auth/me")]
        public async Task<IActionResult> GetMyProfile([FromQuery] string? uid, [FromServices] IChzzkApiClient chzzkApi)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Ok(MooldangBot.Application.Common.Models.Result<object>.Failure("인증되지 않은 사용자입니다."));
            }

            var chzzkUid = uid ?? User.FindFirstValue("StreamerId");
            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(MooldangBot.Application.Common.Models.Result<object>.Failure("치지직 계정 연동 정보가 없습니다."));
            }

            try 
            {
                var channelRes = await chzzkApi.GetChannelsAsync(new[] { chzzkUid });
                var profile = await _db.StreamerProfiles
                                 .IgnoreQueryFilters() 
                                 .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

                if (profile != null)
                {
                    if (channelRes?.Content?.Data?.FirstOrDefault() is { } channelData)
                    {
                        if (!string.IsNullOrEmpty(channelData.ChannelName)) profile.ChannelName = channelData.ChannelName;
                        if (!string.IsNullOrEmpty(channelData.ChannelImageUrl)) profile.ProfileImageUrl = channelData.ChannelImageUrl;
                        await _db.SaveChangesAsync();
                    }

                    return Ok(MooldangBot.Application.Common.Models.Result<object>.Success(new {
                        isAuthenticated = true,
                        isChzzkLinked = !string.IsNullOrEmpty(profile.ChzzkAccessToken),
                        channelName = profile.ChannelName ?? "스트리머",
                        profileImageUrl = profile.ProfileImageUrl ?? "",
                        chzzkUid = profile.ChzzkUid,
                        slug = profile.Slug
                    }));
                }
                else
                {
                    var viewerHash = MooldangBot.Application.Common.Security.Sha256Hasher.ComputeHash(chzzkUid);
                    var viewer = await _db.GlobalViewers.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.ViewerUidHash == viewerHash);
                    if (viewer != null)
                    {
                        return Ok(MooldangBot.Application.Common.Models.Result<object>.Success(new {
                            isAuthenticated = true,
                            isChzzkLinked = false,
                            channelName = viewer.Nickname,
                            profileImageUrl = viewer.ProfileImageUrl ?? "",
                            chzzkUid = viewer.ViewerUid,
                            slug = (string?)null
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AuthMe] Profile refresh failed for {chzzkUid}");
                return Ok(MooldangBot.Application.Common.Models.Result<object>.Failure($"정보 갱신 실패: {ex.Message}"));
            }

            return Ok(MooldangBot.Application.Common.Models.Result<object>.Failure("프로필 정보를 찾을 수 없습니다."));
        }

        [HttpGet("auth/resolve-slug/{slug}")]
        public async Task<IActionResult> ResolveStreamerSlug(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return BadRequest("[오시리스의 거절] 유효하지 않은 주소입니다.");

            // [이지스의 눈]: 레디스 역방향 색인을 통해 고유 ID를 조회합니다.
            var chzzkUid = await _identityCache.GetChzzkUidBySlugAsync(slug);

            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(Result<object>.Failure("[오시리스의 거절] 존재하지 않는 함교 주소입니다."));
            }
            
            return Ok(Result<object>.Success(new { chzzkUid }));
        }

        [HttpGet("auth/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect(BaseDomain);
        }

        [HttpGet("admin/bot/streamers")]
        [Authorize] // 🔐 인증된 사용자라면 일단 목록 조회 허용 (추후 master로 강화 가능)
        public async Task<IActionResult> GetStreamers([FromServices] IChzzkApiClient chzzkApi)
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

            return Ok(Result<object>.Success(result));
        }

        [HttpGet("admin/bot/login")]
        [EnableRateLimiting("strict-auth")]
        public async Task<IActionResult> BotLogin([FromQuery] string? uid)
        {
            try 
            {
                var metadata = await _authService.GenerateAuthMetadataAsync(uid, "bot");
                
                // Double-Submit Cookie
                Response.Cookies.Append(StateCookieName, metadata.State, new CookieOptions 
                { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Lax, 
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
                });

                var sessionData = new AuthSessionData { State = metadata.State, CodeVerifier = metadata.CodeVerifier, TargetUid = uid, LoginType = "bot" };
                await _cache.SetStringAsync($"auth:state:{metadata.State}", JsonSerializer.Serialize(sessionData), new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                });

                return Redirect(metadata.AuthUrl);
            }
            catch (Exception ex)
            {
                return Content($"[봇 인증 오류] {ex.Message}");
            }
        }

        [HttpGet("/api/auth/callback")]
        [HttpGet("/api/v1/auth/callback")]
        [HttpGet("/Auth/callback")] // 레거시 호환용
        [AllowAnonymous]
        public async Task<IActionResult> AuthCallback([FromQuery] string? code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state)) 
                return Content("[오시리스의 거절] 필수 인증 파라미터가 누락되었습니다.");

            // 🛡️ [물멍의 수호]: Double-Submit Cookie 검증
            var stateFromCookie = Request.Cookies[StateCookieName];
            if (string.IsNullOrEmpty(stateFromCookie) || stateFromCookie != state)
            {
                _logger.LogWarning($"[CSRF 감지] State 불일치 (URL: {state}, Cookie: {stateFromCookie})");
                return Content("[보안 경고] 인증 세션이 유효하지 않거나 변조되었습니다. 다시 시도해 주세요.");
            }

            // [분산 캐시]: 세션 데이터 조회
            var cachedJson = await _cache.GetStringAsync($"auth:state:{state}");
            if (string.IsNullOrEmpty(cachedJson))
            {
                return Content("[인증 만료] 인증 시간이 초과되었습니다. 다시 로그인해 주세요.");
            }

            var cachedData = JsonSerializer.Deserialize<AuthSessionData>(cachedJson);
            if (cachedData == null) return Content("[시스템 오류] 인증 세션 데이터가 손상되었습니다.");

            // 🚀 [AuthService]: 핵심 비즈니스 로직(토큰 교환, DB 동기화 등) 처리
            var result = await _authService.ProcessCallbackAsync(code, cachedData);

            if (!result.IsSuccess)
            {
                return Content($"[인증 실패] {result.ErrorMessage}");
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
                return Content(htmlResponse, "text/html; charset=utf-8");
            }

            // 일반 로그인: 역할(RBAC) 조회 및 클레임 생성
            string chzzkUid = result.ChzzkUid!;
            string channelName = result.ChannelName!;
            
            var userRole = cachedData.LoginType == "viewer" ? "viewer" : "streamer";
            var allowedChannels = new List<string> { chzzkUid };

            string masterUid = _configuration["MASTER_UID"] ?? "";
            string botUid = _configuration["BOT_UID"] ?? "";

            // [v6.2.3] 역할 판별 로직 고도화: 의도된 로그인 타입이 streamer인 경우에만 상위 권한 체크
            if (cachedData.LoginType != "viewer")
            {
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

            // [물멍]: 역할에 따른 전용 함교 리다이렉트 (Slug 우선, 없으면 UID 폴백)
            string targetPath = !string.IsNullOrEmpty(result.Slug) ? result.Slug : chzzkUid;
            string redirectPath = $"/{targetPath}/dashboard";
            
            return Redirect($"{BaseDomain.TrimEnd('/')}{redirectPath}");
        }
    }
}
