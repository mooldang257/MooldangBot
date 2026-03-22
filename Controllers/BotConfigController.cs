using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/settings/bot")]
    public class BotConfigController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BotConfigController(AppDbContext db)
        {
            _db = db;
        }

        // 1. 현재 봇 활성화 상태 조회
        [HttpGet("status/{uid}")]
        public async Task<IActionResult> GetBotStatus(string uid)
        {
            var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            return Ok(new { isEnabled = streamer.IsBotEnabled });
        }

        // 2. 봇 활성화 상태 토글
        [HttpPost("toggle/{uid}")]
        public async Task<IActionResult> ToggleBotStatus(string uid, [FromBody] BotToggleRequest req)
        {
            var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            streamer.IsBotEnabled = req.IsEnabled;
            await _db.SaveChangesAsync();

            // 백그라운드 서비스(ChzzkBackgroundService)는 최대 60초 내에 DB를 스캔하여 자동으로 세션을 연결 또는 해제합니다.
            return Ok(new { success = true, isEnabled = streamer.IsBotEnabled, message = "봇 설정이 변경되었습니다. 최대 1분 내에 반영됩니다." });
        }
    }

    public class BotToggleRequest
    {
        public bool IsEnabled { get; set; }
    }
}
