using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Hubs;
using MooldangBot.Contracts.Common.Models;

namespace MooldangBot.Application.Controllers.Config
{
    [ApiController]
    [Route("api/settings/bot")]
    // [v10.1] Primary Constructor ?�용
    public class BotConfigController(IAppDbContext db, IChzzkBotService chzzkService, IIdentityCacheService identityCache) : ControllerBase
    {
        // 1. ?�재 �??�성???�태 조회
        [HttpGet("status/{uid}")]
        public async Task<IActionResult> GetBotStatus(string uid)
        {
            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("?�트리머�?찾을 ???�습?�다."));

            return Ok(Result<object>.Success(new 
            { 
                isEnabled = streamer.IsActive // [v6.1.6] IsActive ?�드 맵핑
            }));
        }

        // 2. �??�성???�태 ?��?
        [HttpPost("toggle/{uid}")]
        public async Task<IActionResult> ToggleBotStatus(string uid, [FromBody] BotToggleRequest req)
        {
            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("?�트리머�?찾을 ???�습?�다."));

            streamer.IsActive = req.IsEnabled; // [v6.1.6] ?�동??Active) 반전
            await db.SaveChangesAsync();

            // 백그?�운???�비?�에 즉시 반영 ?�청
            await chzzkService.RefreshChannelAsync(uid);

            return Ok(Result<object>.Success(new 
            { 
                success = true, 
                isActive = streamer.IsActive, 
                message = "�??�정??즉시 변경되?�습?�다." 
            }));
        }

        // 3. ?�교 주소(Slug) 조회
        [HttpGet("slug/{uid}")]
        public async Task<IActionResult> GetStreamerSlug(string uid)
        {
            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("?�트리머�?찾을 ???�습?�다."));

            return Ok(Result<object>.Success(new { slug = streamer.Slug }));
        }

        // 4. ?�교 주소(Slug) 변�?
        [HttpPost("slug/{uid}")]
        public async Task<IActionResult> UpdateStreamerSlug(string uid, [FromBody] SlugUpdateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Slug)) 
                return BadRequest(Result<string>.Failure("주소??비워?????�습?�다."));
            
            // [물멍]: 보안 �?가?�성???�한 ?�러�??�식 검�?(?�파�? ?�자, ?�이?�만 ?�용)
            var slugPattern = new System.Text.RegularExpressions.Regex("^[a-z0-9-]{3,20}$");
            if (!slugPattern.IsMatch(req.Slug)) 
                return BadRequest(Result<string>.Failure("주소??3~20?�의 ?�문 ?�문?? ?�자, ?�이??-)�??�용?????�습?�다."));

            var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("?�트리머�?찾을 ???�습?�다."));

            // [?�시리스????: 중복 체크
            var isTaken = await db.StreamerProfiles.AnyAsync(p => p.Slug == req.Slug && p.ChzzkUid != uid);
            if (isTaken) 
                return Conflict(Result<string>.Failure("?��? ?�용 중인 ID?�니??"));

            var oldSlug = streamer.Slug;
            streamer.Slug = req.Slug;
            await db.SaveChangesAsync();

            // [?��??�의 ?�화]: 기존 캐시 무효??
            identityCache.InvalidateStreamer(uid);
            if (!string.IsNullOrEmpty(oldSlug)) identityCache.InvalidateSlug(oldSlug);
            identityCache.InvalidateSlug(req.Slug);
            
            return Ok(Result<object>.Success(new 
            { 
                success = true, 
                slug = streamer.Slug, 
                message = "?�교???�로??주소가 ?�록?�었?�니??" 
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
