using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/chatpoint")]
    [Authorize(Policy = "ChannelManager")]
    public class ChatPointController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChatPointController> _logger;

        public ChatPointController(AppDbContext context, ILogger<ChatPointController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetSettings(string chzzkUid)
        {
            _logger.LogInformation("GetSettings called for Uid: {Uid}", chzzkUid);
            var profile = await _context.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound("Streamer not found");

            return Ok(new {
                pointPerChat = profile.PointPerChat,
                pointPerDonation1000 = profile.PointPerDonation1000,
                pointPerAttendance = profile.PointPerAttendance,
                attendanceCommands = profile.AttendanceCommands,
                attendanceReply = profile.AttendanceReply,
                pointCheckCommand = profile.PointCheckCommand,
                pointCheckReply = profile.PointCheckReply
            });
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> SaveSettings(string chzzkUid, [FromBody] ChatPointSettingsDto dto)
        {
            _logger.LogInformation("SaveSettings attempt for Uid: {Uid} by User: {User}", chzzkUid, User.Identity?.Name);
            
            var profile = await _context.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) 
            {
                _logger.LogWarning("Streamer not found for Uid: {Uid}", chzzkUid);
                return NotFound("Streamer not found");
            }

            profile.PointPerChat = dto.PointPerChat;
            profile.PointPerDonation1000 = dto.PointPerDonation1000;
            profile.PointPerAttendance = dto.PointPerAttendance;
            profile.AttendanceCommands = dto.AttendanceCommands ?? "";
            profile.AttendanceReply = dto.AttendanceReply ?? "";
            profile.PointCheckCommand = dto.PointCheckCommand ?? "";
            profile.PointCheckReply = dto.PointCheckReply ?? "";

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("{chzzkUid}/viewers")]
        public async Task<IActionResult> GetViewers(string chzzkUid)
        {
            var viewers = await _context.ViewerProfiles
                .Where(v => v.StreamerChzzkUid == chzzkUid)
                .OrderByDescending(v => v.Points) // 포인트 높은 순 정렬
                .Select(v => new {
                    v.Nickname,
                    v.Points,
                    v.AttendanceCount,
                    v.LastAttendanceAt
                })
                .ToListAsync();

            return Ok(viewers);
        }
    }

    public class ChatPointSettingsDto
    {
        public int PointPerChat { get; set; }
        public int PointPerDonation1000 { get; set; }
        public int PointPerAttendance { get; set; }
        public string? AttendanceCommands { get; set; }
        public string? AttendanceReply { get; set; }
        public string? PointCheckCommand { get; set; }
        public string? PointCheckReply { get; set; }
    }
}
