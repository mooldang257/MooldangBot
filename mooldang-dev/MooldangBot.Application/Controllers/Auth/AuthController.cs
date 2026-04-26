using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Channels;
using MooldangBot.Domain.Common.Security;
using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
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
using MooldangBot.Domain.Common.Models;
using MooldangBot.Application.Services.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Controllers.Auth
{
    [ApiController]
    [Route("api")]
    public class AuthController(
        IAppDbContext _db, 
        IConfiguration _configuration, 
        IAuthService _authService,
        IDistributedCache _cache,
        ILogger<AuthController> _logger) : ControllerBase
    {
        private const string StateCookieName = "__Mooldang_Auth_State";

        private string BaseDomain 
        {
            get {
                var val = _configuration["BASE_DOMAIN"];
                if (string.IsNullOrEmpty(val))
                    throw new Exception("[오시리스의 거절]: 환경 설정 파일(.env 또는 appsettings)에서 'BASE_DOMAIN'이 설정되어 있지 않습니다.");
                
                // [오시리스의 항로]: 프로토콜이 없으면 https://를 자동으로 붙여서 리다이렉트 오류를 방지합니다.
                if (!val.StartsWith("http://") && !val.StartsWith("https://"))
                {
                    val = "https://" + val;
                }
                
                return val;
            }
        }

        [HttpGet("auth/chzzk-login")]
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
                _logger.LogError(ex, "[오시리스의 거절] 로그인 URL 생성 실패");
                return Ok(Result<object>.Failure($"로그인 URL 생성 실패: {ex.Message}"));
            }
        }

        [HttpGet("auth/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect(BaseDomain);
        }

        [HttpGet("auth/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> AuthCallback([FromQuery] string? code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state)) 
            {
                return Ok(Result<object>.Failure("필수 인증 파라미터가 누락되었습니다."));
            }

            var stateFromCookie = Request.Cookies[StateCookieName];
            if (string.IsNullOrEmpty(stateFromCookie) || stateFromCookie != state)
            {
                return Ok(Result<object>.Failure("인증 세션이 유효하지 않거나 변조되었습니다. 다시 시도해 주세요."));
            }

            var cachedJson = await _cache.GetStringAsync($"auth:state:{state}");
            if (string.IsNullOrEmpty(cachedJson))
            {
                return Ok(Result<object>.Failure("인증 시간이 초과되었습니다. 다시 로그인해 주세요."));
            }

            var cachedData = JsonSerializer.Deserialize<AuthSessionData>(cachedJson);
            if (cachedData == null) return Ok(Result<object>.Failure("시스템 오류: 인증 세션 데이터가 손상되었습니다."));

            var result = await _authService.ProcessCallbackAsync(code, cachedData);

            if (!result.IsSuccess)
            {
                return Ok(Result<object>.Failure($"인증 실패: {result.ErrorMessage}"));
            }

            if (!string.IsNullOrEmpty(result.RedirectUrl))
            {
                string htmlResponse = $@"
                    <!DOCTYPE html>
                    <html lang='ko'>
                    <head><meta charset='UTF-8'><title>연동 성공</title></head>
                    <body style='background-color:#121212; color:#00e676; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; text-align:center;'>
                        <div>
                            <h1 style='color:#0093E9;'>치지직 계정 연동 완료!</h1>
                            <p style='color:#fff;'>[{result.ChannelName}] 계정이 물멍 전용 봇으로 등록되었습니다.<br>이제 창을 닫아주세요.</p>
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
                    var viewerHash = MooldangBot.Domain.Common.Security.Sha256Hasher.ComputeHash(chzzkUid);
                    var managedChannels = await _db.CoreStreamerManagers
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
