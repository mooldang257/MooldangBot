using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Services.Auth;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.Auth
{
    /// <summary>
    /// [오시리스의 인장]: 사용자 신원 확인 및 권한 검증을 담당하는 컨트롤러입니다.
    /// (Aegis of Identity): AuthController에서 분리되어 책임이 명확해졌습니다.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class IdentityController(
        IAppDbContext db,
        IChzzkApiClient chzzkApi,
        IIdentityCacheService identityCache,
        IAuthService authService,
        ILogger<IdentityController> logger) : ControllerBase
    {
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile([FromQuery] string? uid)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Ok(Result<object>.Failure("인증되지 않은 사용자입니다."));
            }

            var resolvedUid = uid;
            if (!string.IsNullOrEmpty(uid))
            {
                var uidFromSlug = await identityCache.GetChzzkUidBySlugAsync(uid);
                if (!string.IsNullOrEmpty(uidFromSlug)) resolvedUid = uidFromSlug;
            }

            var chzzkUid = !string.IsNullOrWhiteSpace(resolvedUid) 
                ? resolvedUid 
                : User.FindFirstValue("StreamerId");

            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(Result<object>.Failure("치지직 계정 연동 정보가 없습니다."));
            }

            try 
            {
                var profile = await db.CoreStreamerProfiles
                                 .IgnoreQueryFilters() 
                                 .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid || p.Slug == chzzkUid);

                if (profile != null)
                {
                    var channelRes = await chzzkApi.GetChannelsAsync(new[] { profile.ChzzkUid });
                    
                    if (channelRes?.FirstOrDefault() is { } channelData)
                    {
                        if (!string.IsNullOrEmpty(channelData.ChannelName)) profile.ChannelName = channelData.ChannelName;
                        if (!string.IsNullOrEmpty(channelData.ChannelImageUrl)) profile.ProfileImageUrl = channelData.ChannelImageUrl;
                        await db.SaveChangesAsync();
                    }

                    // [물멍]: 오버레이 주소 생성을 위해 토큰 보강 (없으면 자동 생성)
                    var overlayToken = profile.OverlayToken;
                    if (string.IsNullOrEmpty(overlayToken))
                    {
                        overlayToken = await authService.IssueOverlayTokenAsync(profile.ChzzkUid, "Streamer");
                    }

                    return Ok(Result<object>.Success(new {
                        isAuthenticated = true,
                        isChzzkLinked = !string.IsNullOrEmpty(profile.ChzzkAccessToken),
                        channelName = profile.ChannelName ?? "스트리머",
                        profileImageUrl = profile.ProfileImageUrl ?? "",
                        chzzkUid = profile.ChzzkUid,
                        slug = profile.Slug,
                        role = User.FindFirstValue(ClaimTypes.Role),
                        overlayToken = overlayToken,
                        isActive = profile.IsActive
                    }));
                }
                else
                {
                    // [이지스 통합]: 시청자 정보 조회 시 캐시를 우선 활용합니다.
                    // (조회 시점에는 닉네임을 모르므로 기존 데이터를 유지하기 위해 null 전달 가능하도록 설계됨)
                    var viewerId = await identityCache.SyncGlobalViewerIdAsync(chzzkUid, "viewer"); 
                    var viewer = await db.CoreGlobalViewers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == viewerId);

                    if (viewer != null)
                    {
                        return Ok(Result<object>.Success(new {
                            isAuthenticated = true,
                            isChzzkLinked = false,
                            channelName = viewer.Nickname,
                            profileImageUrl = viewer.ProfileImageUrl ?? "",
                            chzzkUid = viewer.ViewerUid,
                            slug = (string?)null,
                            role = User.FindFirstValue(ClaimTypes.Role)
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[AuthMe] Profile refresh failed for {chzzkUid}");
                return Ok(Result<object>.Failure($"정보 갱신 실패: {ex.Message}"));
            }

            return Ok(Result<object>.Failure("정보를 찾을 수 없습니다."));
        }

        [HttpGet("resolve-slug/{slug}")]
        public async Task<IActionResult> ResolveStreamerSlug(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return BadRequest("[오시리스의 거절] 유효하지 않은 주소입니다.");

            var chzzkUid = await identityCache.GetChzzkUidBySlugAsync(slug);

            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(Result<object>.Failure("[오시리스의 거절] 존재하지 않는 주소입니다."));
            }
            
            return Ok(Result<object>.Success(new { chzzkUid }));
        }

        [HttpGet("validate-access/by-slug/{slug}")]
        [Authorize]
        public async Task<IActionResult> ValidateStreamerAccessBySlug(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return Ok(Result<object>.Failure("[오시리스의 거절] 유효하지 않은 주소입니다."));

            var chzzkUid = await identityCache.GetChzzkUidBySlugAsync(slug);
            if (string.IsNullOrEmpty(chzzkUid))
            {
                return Ok(Result<object>.Failure("[오시리스의 거절] 존재하지 않는 주소입니다."));
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

            return Ok(Result<object>.Failure("[오시리스의 거절] 해당 채널의 관리 권한이 없습니다."));
        }
    }
}
