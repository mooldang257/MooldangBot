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

        // 3. 스트리머 전용 API 설정 조회
        [HttpGet("config/{uid}")]
        public async Task<IActionResult> GetBotConfig(string uid)
        {
            var streamer = await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            // 본인 확인 (또는 마스터)
            var currentUid = User.FindFirst("StreamerId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (currentUid != uid && role != "master") return Forbid();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var defaultRedirectUrl = $"{baseUrl}/Auth/callback";

            return Ok(new 
            { 
                clientId = streamer.ApiClientId,
                clientSecret = streamer.ApiClientSecret,
                redirectUrl = streamer.ApiRedirectUrl ?? defaultRedirectUrl,
                defaultRedirectUrl = defaultRedirectUrl,
                botNickname = streamer.BotNickname // [추가] 연동된 봇 닉네임
            });
        }

        // 4. 스트리머 전용 API 설정 저장
        [HttpPost("config/{uid}")]
        public async Task<IActionResult> UpdateBotConfig(string uid, [FromBody] BotConfigRequest req)
        {
            var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            // 본인 확인 (또는 마스터)
            var currentUid = User.FindFirst("StreamerId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (currentUid != uid && role != "master") return Forbid();

            streamer.ApiClientId = req.ClientId;
            streamer.ApiClientSecret = req.ClientSecret;
            streamer.ApiRedirectUrl = req.RedirectUrl;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "전용 API 설정이 저장되었습니다." });
        }

        // 5. 스트리머 전용 봇 로그인 시작
        [HttpGet("login/{uid}")]
        public async Task<IActionResult> BotLogin(string uid)
        {
            var streamer = await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == uid);
            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            // 본인 확인 (또는 마스터)
            var currentUid = User.FindFirst("StreamerId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (currentUid != uid && role != "master") return Forbid();

            // 💡 AuthController의 로직을 활용하기 위해 리다이렉트
            // AuthController.BotLogin이 이미 uid를 받아 스트리머별 설정을 처리하도록 수정됨
            return Redirect($"/api/admin/bot/login?uid={uid}");
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
}
