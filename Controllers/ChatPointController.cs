using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;

namespace MooldangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatPointController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatPointController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetSettings(string chzzkUid)
        {
            var profile = await _context.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound("Streamer not found");

            return Ok(new {
                pointPerChat = profile.PointPerChat,
                pointPerDonation1000 = profile.PointPerDonation1000,
                pointPerAttendance = profile.PointPerAttendance,
                attendanceCommands = profile.AttendanceCommands
            });
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> SaveSettings(string chzzkUid, [FromBody] ChatPointSettingsDto dto)
        {
            var profile = await _context.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound("Streamer not found");

            profile.PointPerChat = dto.PointPerChat;
            profile.PointPerDonation1000 = dto.PointPerDonation1000;
            profile.PointPerAttendance = dto.PointPerAttendance;
            profile.AttendanceCommands = dto.AttendanceCommands ?? "";

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }

    public class ChatPointSettingsDto
    {
        public int PointPerChat { get; set; }
        public int PointPerDonation1000 { get; set; }
        public int PointPerAttendance { get; set; }
        public string? AttendanceCommands { get; set; }
    }
}
