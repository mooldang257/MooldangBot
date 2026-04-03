using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace MooldangBot.Presentation.Features.ChatPoints
{
    [ApiController]
    [Route("api/chatpoint")]
    [Authorize(Policy = "ChannelManager")]
    public class ChatPointController : ControllerBase
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<ChatPointController> _logger;

        public ChatPointController(IAppDbContext context, ILogger<ChatPointController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetSettings(string chzzkUid)
        {
            _logger.LogInformation("GetSettings called for Uid: {Uid}", chzzkUid);
            // ⭐ [권한 대응] 채널 매니저 권한이 확인된 경우 전역 쿼리 필터를 무시하고 해당 채널 정보를 가져옵니다.
            var profile = await _context.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound("Streamer not found");

            return Ok(new {
                pointPerChat = profile.PointPerChat,
                pointPerDonation1000 = profile.PointPerDonation1000,
                isAutoAccumulateDonation = profile.IsAutoAccumulateDonation
            });
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> SaveSettings(string chzzkUid, [FromBody] ChatPointSettingsDto dto)
        {
            _logger.LogInformation("SaveSettings attempt for Uid: {Uid} by User: {User}", chzzkUid, User.Identity?.Name);
            
            // ⭐ [권한 대응] 채널 매니저 권한이 확인된 경우 전역 쿼리 필터를 무시하고 해당 채널 정보를 가져옵니다.
            var profile = await _context.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) 
            {
                _logger.LogWarning("Streamer not found for Uid: {Uid}", chzzkUid);
                return NotFound("Streamer not found");
            }

            profile.PointPerChat = dto.PointPerChat;
            profile.PointPerDonation1000 = dto.PointPerDonation1000;
            profile.IsAutoAccumulateDonation = dto.IsAutoAccumulateDonation;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("{chzzkUid}/viewers")]
        public async Task<IActionResult> GetViewers(string chzzkUid)
        {
            // ⭐ [권한 대응] 채널 매니저 권한이 확인된 경우 전역 쿼리 필터를 무시하고 해당 채널의 시청자 목록을 가져옵니다.
            var viewers = await _context.StreamerViewers
                .IgnoreQueryFilters()
                .Include(v => v.GlobalViewer)
                .Where(v => v.StreamerProfile!.ChzzkUid == chzzkUid)
                .OrderByDescending(v => v.Points) // 포인트 높은 순 정렬
                .Select(v => new {
                    nickname = v.GlobalViewer!.Nickname,
                    points = v.Points,
                    donationPoints = v.DonationPoints, // [v6.2.1] 추가
                    attendanceCount = v.AttendanceCount,
                    lastAttendanceAt = v.LastAttendanceAt
                })
                .ToListAsync();

            return Ok(viewers);
        }
    }

    public class ChatPointSettingsDto
    {
        [JsonPropertyName("pointPerChat")]
        public int PointPerChat { get; set; }
        
        [JsonPropertyName("pointPerDonation1000")]
        public int PointPerDonation1000 { get; set; }

        [JsonPropertyName("isAutoAccumulateDonation")]
        public bool IsAutoAccumulateDonation { get; set; }
    }
}
