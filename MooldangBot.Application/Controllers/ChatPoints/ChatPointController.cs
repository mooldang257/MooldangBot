using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using MooldangBot.Contracts.Common.Models;

namespace MooldangBot.Application.Controllers.ChatPoints
{
    [ApiController]
    [Route("api/chatpoint")]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor ?ÅÏö©
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
                return NotFound(Result<string>.Failure("?§Ìä∏Î¶¨Î®∏Î•?Ï∞æÏùÑ ???ÜÏäµ?àÎã§."));

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
                return NotFound(Result<string>.Failure("?§Ìä∏Î¶¨Î®∏Î•?Ï∞æÏùÑ ???ÜÏäµ?àÎã§."));
            }

            profile.PointPerChat = dto.PointPerChat;
            profile.PointPerDonation1000 = dto.PointPerDonation1000;
            profile.IsAutoAccumulateDonation = dto.IsAutoAccumulateDonation;

            await context.SaveChangesAsync();
            return Ok(Result<object>.Success(new { success = true, message = "?¨Ïù∏???§Ï†ï???Ä?•Îêò?àÏäµ?àÎã§." }));
        }

        [HttpGet("{chzzkUid}/viewers")]
        public async Task<IActionResult> GetViewers(string chzzkUid)
        {
            // [v7.0] Wallet Architecture: Î∂ÑÏÇ∞??ÏßÄÍ∞??åÏù¥Î∏îÎì§??Ï°∞Ïù∏?òÏó¨ ?µÌï© Î∑??úÍ≥µ
            var viewers = await (from r in context.ViewerRelations.IgnoreQueryFilters()
                                 join p in context.ViewerPoints.IgnoreQueryFilters() 
                                    on new { r.StreamerProfileId, r.GlobalViewerId } equals new { p.StreamerProfileId, p.GlobalViewerId } into points
                                 from p in points.DefaultIfEmpty()
                                 join d in context.ViewerDonations.IgnoreQueryFilters() 
                                    on new { r.StreamerProfileId, r.GlobalViewerId } equals new { d.StreamerProfileId, d.GlobalViewerId } into donations
                                 from d in donations.DefaultIfEmpty()
                                 where r.StreamerProfile!.ChzzkUid == chzzkUid
                                 orderby (p != null ? p.Points : 0) descending
                                 select new {
                                     nickname = r.GlobalViewer!.Nickname,
                                     points = p != null ? p.Points : 0,
                                     donationPoints = d != null ? d.Balance : 0,
                                     attendanceCount = r.AttendanceCount,
                                     lastAttendanceAt = r.LastAttendanceAt
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
