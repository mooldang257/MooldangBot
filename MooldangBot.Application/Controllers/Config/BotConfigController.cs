using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Hubs;
using MooldangBot.Domain.Common.Models;
using Microsoft.Extensions.Configuration;

namespace MooldangBot.Application.Controllers.Config
{
    [ApiController]
    [Route("api/config/bot/{uid}")]
    // [v10.1] Primary Constructor 활용
    public class BotConfigController(
        IAppDbContext db, 
        IChzzkBotService chzzkService, 
        IIdentityCacheService identityCache,
        IConfiguration configuration) : ControllerBase
    {
        // 1. 현재 봇 활성 상태 조회
        [HttpGet("status")]
        public async Task<IActionResult> GetBotStatus([FromRoute] string uid)
        {
            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            return Ok(Result<object>.Success(new 
            { 
                isEnabled = streamer.IsActive 
            }));
        }

        // 2. 봇 활성 상태 변경
        [HttpPatch("status")]
        public async Task<IActionResult> ToggleBotStatus([FromRoute] string uid, [FromBody] BotToggleRequest req)
        {
            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            streamer.IsActive = req.IsEnabled;
            await db.SaveChangesAsync();

            // 백그라운드 서비스에 즉시 반영 요청
            await chzzkService.RefreshChannelAsync(uid);

            return Ok(Result<object>.Success(new 
            { 
                success = true, 
                isActive = streamer.IsActive, 
                message = "설정이 즉시 변경되었습니다." 
            }));
        }

        // 3. 함교 주소(Slug) 조회
        [HttpGet("slug")]
        public async Task<IActionResult> GetStreamerSlug([FromRoute] string uid)
        {
            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var baseDomain = configuration["BASE_DOMAIN"] ?? "https://mooldang.tv";
            return Ok(Result<object>.Success(new 
            { 
                slug = streamer.Slug, 
                baseDomain = baseDomain 
            }));
        }

        // 4. 함교 주소(Slug) 변경
        [HttpPatch("slug")]
        public async Task<IActionResult> UpdateStreamerSlug([FromRoute] string uid, [FromBody] SlugUpdateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Slug)) 
                return BadRequest(Result<string>.Failure("주소는 비워둘 수 없습니다."));
            
            // [물멍]: 보안 및 가독성을 위한 슬러그 형식 검증(알파벳 소문자, 숫자, 하이픈만 허용)
            var slugPattern = new System.Text.RegularExpressions.Regex("^[a-z0-9-]{3,20}$");
            if (!slugPattern.IsMatch(req.Slug)) 
                return BadRequest(Result<string>.Failure("주소는 3~20자의 영문 소문자, 숫자, 하이픈(-)만 사용할 수 있습니다."));

            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // 중복 체크
            var isTaken = await db.StreamerProfiles.AnyAsync(p => p.Slug == req.Slug && p.ChzzkUid != uid);
            if (isTaken) 
                return Conflict(Result<string>.Failure("이미 사용 중인 주소입니다."));

            var oldSlug = streamer.Slug;
            streamer.Slug = req.Slug;
            await db.SaveChangesAsync();

            // [안정성의 평화]: 기존 캐시 무효화
            identityCache.InvalidateStreamer(uid);
            if (!string.IsNullOrEmpty(oldSlug)) identityCache.InvalidateSlug(oldSlug);
            identityCache.InvalidateSlug(req.Slug);
            
            return Ok(Result<object>.Success(new 
            { 
                success = true, 
                slug = streamer.Slug, 
                message = "함교의 새로운 주소가 등록되었습니다." 
            }));
        }

        // 5. 전용 API 설정 조회
        [HttpGet("config")]
        public async Task<IActionResult> GetApiConfig([FromRoute] string uid)
        {
            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            return Ok(Result<object>.Success(new 
            { 
                clientId = streamer.ClientId,
                clientSecret = streamer.ClientSecret,
                redirectUrl = streamer.RedirectUrl,
                botNickname = streamer.BotNickname
            }));
        }

        // 6. 전용 API 설정 저장
        [HttpPatch("config")]
        public async Task<IActionResult> UpdateApiConfig([FromRoute] string uid, [FromBody] BotConfigRequest req)
        {
            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            streamer.ClientId = req.ClientId;
            streamer.ClientSecret = req.ClientSecret;
            streamer.RedirectUrl = req.RedirectUrl;

            await db.SaveChangesAsync();
            return Ok(Result<bool>.Success(true));
        }

        // 7. 전용 봇 계정 로그인 (치지직 리다이렉트)
        [HttpGet("login")]
        public async Task<IActionResult> BotLogin([FromRoute] string uid)
        {
            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null || string.IsNullOrEmpty(streamer.ClientId)) 
                return BadRequest(Result<string>.Failure("API 설정이 완료되지 않았습니다."));

            var state = Guid.NewGuid().ToString();
            var authUrl = $"https://chzzk.naver.com/oauth2/v1/authorize?client_id={streamer.ClientId}&redirect_uri={streamer.RedirectUrl}&response_type=code&state={state}";
            
            return Redirect(authUrl);
        }
    }

    public class BotToggleRequest
    {
        public bool IsEnabled { get; set; }
    }

    public class BotConfigRequest
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? RedirectUrl { get; set; }
    }

    public class SlugUpdateRequest
    {
        public string Slug { get; set; } = string.Empty;
    }
}
