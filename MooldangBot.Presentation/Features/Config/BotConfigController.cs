using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Workers;
using MooldangBot.Presentation.Hubs;
using System.Security.Claims;

namespace MooldangBot.Presentation.Features.Config
{
    [ApiController]
    [Route("api/settings/bot")]
    public class BotConfigController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IChzzkBotService _chzzkService;

        public BotConfigController(IAppDbContext db, IChzzkBotService chzzkService)
        {
            _db = db;
            _chzzkService = chzzkService;
        }

        // 1. 현재 봇 활성화 상태 조회
        [HttpGet("status/{uid}")]
        public async Task<IActionResult> GetBotStatus(string uid)
        {
            var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            return Ok(new 
            { 
                isEnabled = streamer.IsActive // [v6.1.6] IsActive 필드 맵핑
            });
        }

        // 2. 봇 활성화 상태 토글
        [HttpPost("toggle/{uid}")]
        public async Task<IActionResult> ToggleBotStatus(string uid, [FromBody] BotToggleRequest req)
        {
            var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            streamer.IsActive = req.IsEnabled; // [v6.1.6] 활동성(Active) 반전
            await _db.SaveChangesAsync();

            // 백그라운드 서비스에 즉시 반영 요청
            await _chzzkService.RefreshChannelAsync(uid);

            return Ok(new { success = true, isActive = streamer.IsActive, message = "봇 설정이 즉시 변경되었습니다." });
        }

        // 3. [v6.2] 개별 API 설정 및 전용 봇 로그인 기능은 더 이상 지원되지 않습니다.
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
}
