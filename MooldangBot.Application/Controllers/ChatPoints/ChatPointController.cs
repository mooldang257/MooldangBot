using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.ChatPoints
{
    [ApiController]
    [Route("api/chatpoint")]
    [Authorize(Policy = "ChannelManager")]
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
                pointPerAttendance = profile.PointPerAttendance,
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
            profile.PointPerAttendance = dto.PointPerAttendance;
            profile.IsAutoAccumulateDonation = dto.IsAutoAccumulateDonation;

            await context.SaveChangesAsync();
            return Ok(Result<object>.Success(new { success = true, message = "포인트 설정이 저장되었습니다." }));
        }

        [HttpGet("{chzzkUid}/viewers")]
        public async Task<IActionResult> GetViewers(
            string chzzkUid, 
            [FromQuery] string? search = null, 
            [FromQuery] string? sort = "points", 
            [FromQuery] int offset = 0, 
            [FromQuery] int limit = 20)
        {
            var query = context.ViewerRelations
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(r => r.StreamerProfile!.ChzzkUid == chzzkUid)
                        .Select(r => new {
                            nickname = r.GlobalViewer!.Nickname,
                            points = context.ViewerPoints
                                .IgnoreQueryFilters()
                                .Where(p => p.StreamerProfileId == r.StreamerProfileId && p.GlobalViewerId == r.GlobalViewerId)
                                .Select(p => (int?)p.Points)
                                .FirstOrDefault() ?? 0,
                            attendanceCount = r.AttendanceCount,
                            lastAttendanceAt = r.LastAttendanceAt
                        });

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(v => v.nickname.Contains(search));
            }

            query = sort switch
            {
                "attendance" => query.OrderByDescending(v => v.attendanceCount),
                "recent" => query.OrderByDescending(v => v.lastAttendanceAt),
                _ => query.OrderByDescending(v => v.points)
            };

            var total = await query.CountAsync();
            var items = await query.Skip(offset).Take(limit).ToListAsync();

            return Ok(Result<object>.Success(new { total, items }));
        }

        [HttpGet("{chzzkUid}/donations")]
        public async Task<IActionResult> GetDonations(
            string chzzkUid, 
            [FromQuery] string? search = null, 
            [FromQuery] string? sort = "total", 
            [FromQuery] int offset = 0, 
            [FromQuery] int limit = 20)
        {
            var query = context.ViewerDonations
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(d => d.StreamerProfile!.ChzzkUid == chzzkUid)
                        .Select(d => new {
                            nickname = d.GlobalViewer!.Nickname,
                            balance = d.Balance,
                            totalDonated = d.TotalDonated,
                            updatedAt = d.UpdatedAt ?? d.CreatedAt
                        });

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(v => v.nickname.Contains(search));
            }

            query = sort switch
            {
                "balance" => query.OrderByDescending(v => v.balance),
                "recent" => query.OrderByDescending(v => v.updatedAt),
                _ => query.OrderByDescending(v => v.totalDonated)
            };

            var total = await query.CountAsync();
            var items = await query.Skip(offset).Take(limit).ToListAsync();

            return Ok(Result<object>.Success(new { total, items }));
        }
    }

    public class ChatPointSettingsDto
    {
        [JsonPropertyName("pointPerChat")]
        public int PointPerChat { get; set; }
        
        [JsonPropertyName("pointPerDonation1000")]
        public int PointPerDonation1000 { get; set; }

        [JsonPropertyName("pointPerAttendance")]
        public int PointPerAttendance { get; set; }

        [JsonPropertyName("isAutoAccumulateDonation")]
        public bool IsAutoAccumulateDonation { get; set; }
    }
}
