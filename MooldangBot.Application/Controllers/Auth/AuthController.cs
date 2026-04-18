using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Contracts.Security;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Application.Services.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Controllers.Auth
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api")]
    [Route("api/v{version:apiVersion}")]
    public class AuthController(
        IAppDbContext _db, 
        IConfiguration _configuration, 
        IChzzkApiClient _chzzkApi, 
        IAuthService _authService,
        IIdentityCacheService _identityCache,
        IDistributedCache _cache,
        IHttpClientFactory _httpClientFactory,
        ILogger<AuthController> _logger) : ControllerBase
    {
        private const string StateCookieName = "__Mooldang_Auth_State";

        private string BaseDomain 
        {
            get {
                var val = _configuration["BASE_DOMAIN"];
                if (!string.IsNullOrEmpty(val)) return val;
                
                throw new Exception("[?�시리스??거절]: ?�경 ?�정 ?�일(.env ?�는 appsettings)?�서 'BASE_DOMAIN'???�정?�어 ?��? ?�습?�다.");
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
                var metadata = await _authService.GenerateAuthMetadataAsync(null, loginType);
                
                Response.Cookies.Append(StateCookieName, metadata.State, new CookieOptions 
                { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Lax, 
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
                });

                var sessionData = new AuthSessionData { 
                    State = metadata.State, 
                    CodeVerifier = metadata.CodeVerifier,
                    LoginType = loginType
                };
                var json = JsonSerializer.Serialize(sessionData);
                await _cache.SetStringAsync($"auth:state:{metadata.State}", json, new DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                });

                return Redirect(metadata.AuthUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[?�시리스??거절] 로그??URL ?�성 ?�패");
                return Ok(Result<object>.Failure($"로그??URL ?�성 ?�패: {ex.Message}"));
            }
        }

        [HttpGet("proxy/image")]
        public async Task<IActionResult> ProxyImage([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url)) return NotFound();

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
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Referer", "https://chzzk.naver.com/");
                
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return NotFound();
                
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                var stream = await response.Content.ReadAsStreamAsync();
                
                return File(stream, contentType);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpGet("auth/me")]
        public async Task<IActionResult> GetMyProfile([FromQuery] string? uid)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Ok(Result<object>.Failure("?�증?��? ?��? ?�용?�입?�다."));
            }

            var resolvedUid = uid;
            if (!string.IsNullOrEmpty(uid))
            {
                var uidFromSlug = await _identityCache.GetChzzkUidBySlugAsync(uid);
                if (!string.IsNullOrEmpty(uidFromSlug)) resolvedUid = uidFromSlug;
            }

            var chzzkUid = !string.IsNullOrWhiteSpace(resolvedUid) 
                ? resolvedUid 
                : User.FindFirstValue("StreamerId");

            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(Result<object>.Failure("치�?�?계정 ?�동 ?�보가 ?�습?�다."));
            }

            try 
            {
                var profile = await _db.StreamerProfiles
                                 .IgnoreQueryFilters() 
                                 .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid || p.Slug == chzzkUid);

                if (profile != null)
                {
                    var channelRes = await _chzzkApi.GetChannelsAsync(new[] { profile.ChzzkUid });
                    
                    if (channelRes?.FirstOrDefault() is { } channelData)
                    {
                        if (!string.IsNullOrEmpty(channelData.ChannelName)) profile.ChannelName = channelData.ChannelName;
                        if (!string.IsNullOrEmpty(channelData.ChannelImageUrl)) profile.ProfileImageUrl = channelData.ChannelImageUrl;
                        await _db.SaveChangesAsync();
                    }

                    return Ok(Result<object>.Success(new {
                        isAuthenticated = true,
                        isChzzkLinked = !string.IsNullOrEmpty(profile.ChzzkAccessToken),
                        channelName = profile.ChannelName ?? "?�트리머",
                        profileImageUrl = profile.ProfileImageUrl ?? "",
                        chzzkUid = profile.ChzzkUid,
                        slug = profile.Slug
                    }));
                }
                else
                {
                    var viewerHash = MooldangBot.Contracts.Security.Sha256Hasher.ComputeHash(chzzkUid);
                    var viewer = await _db.GlobalViewers.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.ViewerUidHash == viewerHash);
                    if (viewer != null)
                    {
                        return Ok(Result<object>.Success(new {
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
                return Ok(Result<object>.Failure($"?�보 갱신 ?�패: {ex.Message}"));
            }

            return Ok(Result<object>.Failure("?�로???�보�?찾을 ???�습?�다."));
        }

        [HttpGet("auth/resolve-slug/{slug}")]
        public async Task<IActionResult> ResolveStreamerSlug(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return BadRequest("[?�시리스??거절] ?�효?��? ?��? 주소?�니??");

            var chzzkUid = await _identityCache.GetChzzkUidBySlugAsync(slug);

            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(Result<object>.Failure("[?�시리스??거절] 존재?��? ?�는 주소?�니??"));
            }
            
            return Ok(Result<object>.Success(new { chzzkUid }));
        }

        [HttpGet("auth/validate-access/by-slug/{slug}")]
        [Authorize]
        public async Task<IActionResult> ValidateStreamerAccessBySlug(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return Ok(Result<object>.Failure("[?�시리스??거절] ?�효?��? ?��? 주소?�니??"));

            var chzzkUid = await _identityCache.GetChzzkUidBySlugAsync(slug);
            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(Result<object>.Failure("[?�시리스??거절] 존재?��? ?�는 주소?�니??"));
            }

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = User.FindFirstValue("StreamerId");

            if (userRole == "master")
            {
                return Ok(Result<object>.Success(new { chzzkUid }));
            }

            if (currentUserId?.ToLower() == chzzkUid.ToLower())
            {
                return Ok(Result<object>.Success(new { chzzkUid }));
            }

            var allowedChannels = User.FindAll("AllowedChannelId").Select(c => c.Value.ToLower()).ToList();
            if (allowedChannels.Contains(chzzkUid.ToLower()))
            {
                return Ok(Result<object>.Success(new { chzzkUid }));
            }

            return Ok(Result<object>.Failure("[?�시리스??거절] ?�당 채널??관�?권한???�습?�다."));
        }

        [HttpGet("auth/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect(BaseDomain);
        }

        [HttpGet("admin/bot/streamers")]
        [Authorize] 
        public async Task<IActionResult> GetStreamers()
        {
            var streamersInDb = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .ToListAsync();

            if (streamersInDb.Any())
            {
                var uids = streamersInDb.Select(s => s.ChzzkUid).ToList();
                for (int i = 0; i < uids.Count; i += 20)
                {
                    var chunk = uids.Skip(i).Take(20).ToList();
                    var channelRes = await _chzzkApi.GetChannelsAsync(chunk);

                    if (channelRes != null)
                    {
                        foreach (var channelData in channelRes)
                        {
                            var target = streamersInDb.FirstOrDefault(s => s.ChzzkUid == channelData.ChannelId);
                            if (target != null)
                            {
                                if (!string.IsNullOrEmpty(channelData.ChannelName)) target.ChannelName = channelData.ChannelName;
                                if (!string.IsNullOrEmpty(channelData.ChannelImageUrl)) target.ProfileImageUrl = channelData.ChannelImageUrl;
                            }
                        }
                    }
                }
                await _db.SaveChangesAsync();
            }

            var result = streamersInDb
                .OrderByDescending(p => p.Id)
                .Select(p => new {
                    chzzkUid = p.ChzzkUid,
                    channelName = p.ChannelName,
                    profileUrl = p.ProfileImageUrl,
                    isActive = p.IsActive,
                    isMasterEnabled = p.IsMasterEnabled,
                    lastActiveAt = p.TokenExpiresAt
                })
                .ToList();

            return Ok(Result<ListResponse<object>>.Success(new ListResponse<object>(result, result.Count)));
        }

        [HttpGet("admin/bot/login")]
        [EnableRateLimiting("strict-auth")]
        public async Task<IActionResult> BotLogin([FromQuery] string? uid)
        {
            try 
            {
                var metadata = await _authService.GenerateAuthMetadataAsync(uid, "bot");
                
                Response.Cookies.Append(StateCookieName, metadata.State, new CookieOptions 
                { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Lax, 
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
                });

                var sessionData = new AuthSessionData { State = metadata.State, CodeVerifier = metadata.CodeVerifier, TargetUid = uid, LoginType = "bot" };
                await _cache.SetStringAsync($"auth:state:{metadata.State}", JsonSerializer.Serialize(sessionData), new DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                });

                return Redirect(metadata.AuthUrl);
            }
            catch (Exception ex)
            {
                return Content($"[�??�증 ?�류] {ex.Message}");
            }
        }

        [HttpGet("/api/auth/callback")]
        [HttpGet("/api/v1/auth/callback")]
        [HttpGet("/Auth/callback")] // ?�� [Aegis Bridge]: Nginx ?�회 경로 지??(404 방�?)
        [AllowAnonymous]
        public async Task<IActionResult> AuthCallback([FromQuery] string? code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state)) 
            {
                return Ok(Result<object>.Failure("?�수 ?�증 ?�라미터가 ?�락?�었?�니??"));
            }

            var stateFromCookie = Request.Cookies[StateCookieName];
            if (string.IsNullOrEmpty(stateFromCookie) || stateFromCookie != state)
            {
                return Ok(Result<object>.Failure("?�증 ?�션???�효?��? ?�거??변조되?�습?�다. ?�시 ?�도??주세??"));
            }

            var cachedJson = await _cache.GetStringAsync($"auth:state:{state}");
            if (string.IsNullOrEmpty(cachedJson))
            {
                return Ok(Result<object>.Failure("?�증 ?�간??초과?�었?�니?? ?�시 로그?�해 주세??"));
            }

            var cachedData = JsonSerializer.Deserialize<AuthSessionData>(cachedJson);
            if (cachedData == null) return Ok(Result<object>.Failure("?�스???�류: ?�증 ?�션 ?�이?��? ?�상?�었?�니??"));

            var result = await _authService.ProcessCallbackAsync(code, cachedData);

            if (!result.IsSuccess)
            {
                return Ok(Result<object>.Failure($"?�증 ?�패: {result.ErrorMessage}"));
            }

            if (!string.IsNullOrEmpty(result.RedirectUrl))
            {
                string htmlResponse = $@"
                    <!DOCTYPE html>
                    <html lang='ko'>
                    <head><meta charset='UTF-8'><title>�??�동 ?�공</title></head>
                    <body style='background-color:#121212; color:#00e676; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; text-align:center;'>
                        <div>
                            <h1 style='color:#0093E9;'>?�� �?계정 ?�동 ?�료!</h1>
                            <p style='color:#fff;'>[{result.ChannelName}] 계정??물멍 ?�용 봇으�??�록?�었?�니??<br>?�제 창을 ?�아주세??</p>
                        </div>
                    </body>
                    </html>";
                return Content(htmlResponse, "text/html; charset=utf-8");
            }

            string chzzkUid = result.ChzzkUid!;
            string channelName = result.ChannelName!;
            
            var userRole = cachedData.LoginType == "viewer" ? "viewer" : "streamer";
            var allowedChannels = new List<string> { chzzkUid };

            string masterUid = _configuration["MASTER_UID"] ?? "";
            string botUid = _configuration["BOT_UID"] ?? "";

            if (cachedData.LoginType != "viewer")
            {
                if ((!string.IsNullOrEmpty(masterUid) && chzzkUid == masterUid) || 
                    (!string.IsNullOrEmpty(botUid) && chzzkUid == botUid))
                {
                    userRole = "master";
                }
                else
                {
                    var viewerHash = MooldangBot.Contracts.Security.Sha256Hasher.ComputeHash(chzzkUid);
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

            Response.Cookies.Delete(StateCookieName);
            await _cache.RemoveAsync($"auth:state:{state}");

            string targetPath = !string.IsNullOrEmpty(result.Slug) ? result.Slug : chzzkUid;
            string redirectUri = $"{BaseDomain.TrimEnd('/')}/{targetPath}/dashboard";
            
            return Redirect(redirectUri);
        }
    }
}
