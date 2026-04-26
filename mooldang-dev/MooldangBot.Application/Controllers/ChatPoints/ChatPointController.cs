using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Extensions;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Controllers.ChatPoints
{
    [ApiController]
    [Route("api/chat-point")]
    [Authorize(Policy = "ChannelManager")]
    public class ChatPointController(
        IAppDbContext context, 
        ILogger<ChatPointController> logger,
        IIdentityCacheService identityCache) : ControllerBase
    {
        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetSettings(string chzzkUid)
        {
            var profile = await GetCachedProfileAsync(chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            return Ok(Result<ChatPointSettingsDto>.Success(new ChatPointSettingsDto {
                PointPerChat = profile.PointPerChat,
                PointPerDonation1000 = profile.PointPerDonation1000,
                PointPerAttendance = profile.PointPerAttendance,
                IsAutoAccumulateDonation = profile.IsAutoAccumulateDonation
            }));
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> SaveSettings(string chzzkUid, [FromBody] ChatPointSettingsDto dto)
        {
            logger.LogInformation("SaveSettings attempt for Uid: {Uid} by User: {User}", chzzkUid, User.Identity?.Name);
            
            var profile = await context.CoreStreamerProfiles
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
            [FromQuery] CursorPagedRequest request)
        {
            // [v10.8] 안정성을 위해 서브쿼리 대신 명시적 조인 방식으로 복구하되 AsNoTracking으로 성능 최적화
            var query = from r in context.CoreViewerRelations.AsNoTracking().IgnoreQueryFilters()
                        join g in context.CoreGlobalViewers.AsNoTracking().IgnoreQueryFilters() on r.GlobalViewerId equals g.Id
                        join p in context.FuncViewerPoints.AsNoTracking().IgnoreQueryFilters() 
                           on new { r.StreamerProfileId, r.GlobalViewerId } equals new { p.StreamerProfileId, p.GlobalViewerId } into pts
                        from p in pts.DefaultIfEmpty()
                        where r.StreamerProfile!.ChzzkUid == chzzkUid
                        select new {
                            id = r.Id,
                            nickname = g.Nickname,
                            points = p != null ? p.Points : 0,
                            attendanceCount = r.AttendanceCount,
                            consecutiveAttendanceCount = r.ConsecutiveAttendanceCount,
                            lastAttendanceAt = r.LastAttendanceAt
                        };

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v => v.nickname.Contains(request.Search));
            }

            // [물멍]: 커서 조건 적용 (Id 기반으로 안정성 확보)
            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                query = query.Where(v => v.id < request.Cursor.Value);
            }

            query = request.Sort switch
            {
                "attendance" => query.OrderByDescending(v => v.attendanceCount).ThenByDescending(v => v.id),
                "consecutive" => query.OrderByDescending(v => v.consecutiveAttendanceCount).ThenByDescending(v => v.id),
                "recent" => query.OrderByDescending(v => v.lastAttendanceAt).ThenByDescending(v => v.id),
                _ => query.OrderByDescending(v => v.points).ThenByDescending(v => v.id)
            };

            var items = await query.Select(v => new ViewerPointResponseDto(
                v.id,
                v.nickname,
                v.points,
                v.attendanceCount,
                v.consecutiveAttendanceCount,
                v.lastAttendanceAt
            )).ToPagedListAsync(request.Limit, v => v.Id);

            return Ok(Result<CursorPagedResponse<ViewerPointResponseDto>>.Success(items));
        }

        [HttpGet("{chzzkUid}/donations")]
        public async Task<IActionResult> GetDonations(
            string chzzkUid, 
            [FromQuery] CursorPagedRequest request)
        {
            // [v10.8] 안정성을 위해 서브쿼리 대신 명시적 조인 방식으로 복구하되 AsNoTracking으로 성능 최적화
            var query = context.FuncViewerDonations
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(d => d.StreamerProfile!.ChzzkUid == chzzkUid)
                        .Select(d => new {
                            id = d.Id,
                            nickname = d.GlobalViewer!.Nickname,
                            balance = d.Balance,
                            totalDonated = d.TotalDonated,
                            updatedAt = d.UpdatedAt ?? d.CreatedAt
                        });

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v => v.nickname.Contains(request.Search));
            }

            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                query = query.Where(v => v.id < request.Cursor.Value);
            }

            query = request.Sort switch
            {
                "balance" => query.OrderByDescending(v => v.balance).ThenByDescending(v => v.id),
                "recent" => query.OrderByDescending(v => v.updatedAt).ThenByDescending(v => v.id),
                _ => query.OrderByDescending(v => v.totalDonated).ThenByDescending(v => v.id)
            };

            var items = await query.Select(d => new ViewerDonationResponseDto(
                d.id,
                d.nickname,
                d.balance,
                d.totalDonated,
                d.updatedAt
            )).ToPagedListAsync(request.Limit, d => d.Id);

            return Ok(Result<CursorPagedResponse<ViewerDonationResponseDto>>.Success(items));
        }

        private async Task<StreamerProfile?> GetCachedProfileAsync(string uid)
        {
            var profile = await identityCache.GetStreamerProfileAsync(uid);
            if (profile != null) return profile;

            return await context.CoreStreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == uid);
        }
    }

    public record ViewerPointResponseDto(long Id, string Nickname, long Points, int AttendanceCount, int ConsecutiveAttendanceCount, KstClock? LastAttendanceAt);
    public record ViewerDonationResponseDto(long Id, string Nickname, int Balance, long TotalDonated, KstClock? UpdatedAt);

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
