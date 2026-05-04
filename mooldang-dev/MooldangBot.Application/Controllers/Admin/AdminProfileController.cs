using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Application.Services.Auth;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Application.Controllers.Admin
{
    /// <summary>
    /// [오시리스의 옥좌]: 마스터 전용 봇 및 스트리머 관리 기능을 담당하는 컨트롤러입니다.
    /// </summary>
    [ApiController]
    [Route("api/admin/bot")]
    [Authorize]
    public class AdminProfileController(
        IAppDbContext db,
        IChzzkApiClient chzzkApi,
        IAuthService authService,
        IDistributedCache cache) : ControllerBase
    {
        private const string StateCookieName = "__Mooldang_Auth_State";

        [HttpGet("streamers")]
        public async Task<IActionResult> GetStreamers()
        {
            var streamersInDb = await db.TableCoreStreamerProfiles
                .IgnoreQueryFilters()
                .ToListAsync();

            if (streamersInDb.Any())
            {
                var uids = streamersInDb.Select(s => s.ChzzkUid).ToList();
                for (int i = 0; i < uids.Count; i += 20)
                {
                    var chunk = uids.Skip(i).Take(20).ToList();
                    var channelRes = await chzzkApi.GetChannelsAsync(chunk);

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
                await db.SaveChangesAsync();
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

        [HttpGet("login")]
        public async Task<IActionResult> BotLogin([FromQuery] string? uid)
        {
            try 
            {
                var metadata = await authService.GenerateAuthMetadataAsync(uid, "bot");
                
                Response.Cookies.Append(StateCookieName, metadata.State, new CookieOptions 
                { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Lax, 
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
                });

                var sessionData = new AuthSessionData { State = metadata.State, CodeVerifier = metadata.CodeVerifier, TargetUid = uid, LoginType = "bot" };
                await cache.SetStringAsync($"auth:state:{metadata.State}", JsonSerializer.Serialize(sessionData), new DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                });

                return Redirect(metadata.AuthUrl);
            }
            catch (Exception ex)
            {
                return Content($"[인증 오류] {ex.Message}");
            }
        }
    }
}
