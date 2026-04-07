using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Workers;
using MooldangBot.Presentation.Hubs;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.Config
{
    [ApiController]
    [Route("api/settings/bot")]
    // [v10.1] Primary Constructor 적용
    public class BotConfigController(IAppDbContext db, IChzzkBotService chzzkService, IIdentityCacheService identityCache) : ControllerBase
    {
        // 1. 현재 봇 활성화 상태 조회
        [HttpGet("status/{uid}")]
        public async Task<IActionResult> GetBotStatus(string uid)
        {
            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            return Ok(Result<object>.Success(new 
            { 
                isEnabled = streamer.IsActive // [v6.1.6] IsActive 필드 맵핑
            }));
        }

        // 2. 봇 활성화 상태 토글
        [HttpPost("toggle/{uid}")]
        public async Task<IActionResult> ToggleBotStatus(string uid, [FromBody] BotToggleRequest req)
        {
            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            streamer.IsActive = req.IsEnabled; // [v6.1.6] 활동성(Active) 반전
            await db.SaveChangesAsync();

            // 백그라운드 서비스에 즉시 반영 요청
            await chzzkService.RefreshChannelAsync(uid);

            return Ok(Result<object>.Success(new 
            { 
                success = true, 
                isActive = streamer.IsActive, 
                message = "봇 설정이 즉시 변경되었습니다." 
            }));
        }

        // 3. 함교 주소(Slug) 조회
        [HttpGet("slug/{uid}")]
        public async Task<IActionResult> GetStreamerSlug(string uid)
        {
            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            return Ok(Result<object>.Success(new { slug = streamer.Slug }));
        }

        // 4. 함교 주소(Slug) 변경
        [HttpPost("slug/{uid}")]
        public async Task<IActionResult> UpdateStreamerSlug(string uid, [FromBody] SlugUpdateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Slug)) 
                return BadRequest(Result<string>.Failure("주소는 비워둘 수 없습니다."));
            
            // [물멍]: 보안 및 가독성을 위한 슬러그 형식 검증 (알파벳, 숫자, 하이픈만 허용)
            var slugPattern = new System.Text.RegularExpressions.Regex("^[a-z0-9-]{3,20}$");
            if (!slugPattern.IsMatch(req.Slug)) 
                return BadRequest(Result<string>.Failure("주소는 3~20자의 영문 소문자, 숫자, 하이픈(-)만 사용할 수 있습니다."));

            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // [오시리스의 눈]: 중복 체크
            var isTaken = await db.StreamerProfiles.AnyAsync(p => p.Slug == req.Slug && p.ChzzkUid != uid);
            if (isTaken) 
                return Conflict(Result<string>.Failure("이미 사용 중인 ID입니다."));

            var oldSlug = streamer.Slug;
            streamer.Slug = req.Slug;
            await db.SaveChangesAsync();

            // [이지스의 정화]: 기존 캐시 무효화
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
