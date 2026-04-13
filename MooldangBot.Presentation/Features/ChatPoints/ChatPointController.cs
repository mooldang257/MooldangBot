using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.ChatPoints
{
    [ApiController]
    [Route("api/chatpoint")]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 적용
    public class ChatPointController(IAppDbContext context, ILogger<ChatPointController> logger) : ControllerBase
    {
        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetSettings(string chzzkUid)
        {
            logger.LogInformation("GetSettings called for Uid: {Uid}", chzzkUid);
            var profile = await context.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            return Ok(Result<object>.Success(new {
                pointPerChat = profile.PointPerChat,
                pointPerDonation1000 = profile.PointPerDonation1000,
                isAutoAccumulateDonation = profile.IsAutoAccumulateDonation
            }));
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> SaveSettings(string chzzkUid, [FromBody] ChatPointSettingsDto dto)
        {
            logger.LogInformation("SaveSettings attempt for Uid: {Uid} by User: {User}", chzzkUid, User.Identity?.Name);
            
            var profile = await context.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            
            if (profile == null) 
            {
                logger.LogWarning("Streamer not found for Uid: {Uid}", chzzkUid);
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));
            }

            profile.PointPerChat = dto.PointPerChat;
            profile.PointPerDonation1000 = dto.PointPerDonation1000;
            profile.IsAutoAccumulateDonation = dto.IsAutoAccumulateDonation;

            await context.SaveChangesAsync();
            return Ok(Result<object>.Success(new { success = true, message = "포인트 설정이 저장되었습니다." }));
        }

        [HttpGet("{chzzkUid}/viewers")]
        public async Task<IActionResult> GetViewers(string chzzkUid)
        {
            var viewers = await context.StreamerViewers
                .IgnoreQueryFilters()
                .Include(v => v.GlobalViewer)
                .Where(v => v.StreamerProfile!.ChzzkUid == chzzkUid)
                .OrderByDescending(v => v.Points) 
                .Select(v => new {
                    nickname = v.GlobalViewer!.Nickname,
                    points = v.Points,
                    donationPoints = v.DonationPoints, 
                    attendanceCount = v.AttendanceCount,
                    lastAttendanceAt = v.LastAttendanceAt
                })
                .ToListAsync();

            return Ok(Result<object>.Success(viewers));
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
