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
            
            var profile = await context.TableCoreStreamerProfiles
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
            
            // [물멍]: 설정이 변경되었으므로 즉시 봇에 반영되도록 캐시 무효화
            identityCache.InvalidateStreamer(chzzkUid);

            return Ok(Result<object>.Success(new { IsSuccess = true, Message = "포인트 설정이 저장되었습니다." }));
        }

        [HttpGet("{chzzkUid}/viewers")]
        public async Task<IActionResult> GetViewers(
            string chzzkUid, 
            [FromQuery] CursorPagedRequest request)
        {
            // [v10.8] 안정성을 위해 서브쿼리 대신 명시적 조인 방식으로 복구하되 AsNoTracking으로 성능 최적화
            var query = from r in context.TableCoreViewerRelations.AsNoTracking().IgnoreQueryFilters()
                        join g in context.TableCoreGlobalViewers.AsNoTracking().IgnoreQueryFilters() on r.GlobalViewerId equals g.Id
                        join p in context.TableFuncViewerPoints.AsNoTracking().IgnoreQueryFilters() 
                           on new { r.StreamerProfileId, r.GlobalViewerId } equals new { p.StreamerProfileId, p.GlobalViewerId } into pts
                        from p in pts.DefaultIfEmpty()
                        where r.CoreStreamerProfiles!.ChzzkUid == chzzkUid
                        group new { r, g, p } by r.Id into grouped
                        select new {
                            Id = grouped.Key,
                            Nickname = grouped.Max(x => x.g.Nickname),
                            Points = grouped.Sum(x => x.p != null ? x.p.Points : 0),
                            AttendanceCount = grouped.Max(x => x.r.AttendanceCount),
                            ConsecutiveAttendanceCount = grouped.Max(x => x.r.ConsecutiveAttendanceCount),
                            LastAttendanceAt = grouped.Max(x => x.r.LastAttendanceAt)
                        };

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v => v.Nickname.Contains(request.Search));
            }

            // [물멍]: 커서 조건 적용 (Id 기반으로 안정성 확보)
            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                query = query.Where(v => v.Id < request.Cursor.Value);
            }

            query = request.Sort switch
            {
                "attendance" => query.OrderByDescending(v => v.AttendanceCount).ThenByDescending(v => v.Id),
                "consecutive" => query.OrderByDescending(v => v.ConsecutiveAttendanceCount).ThenByDescending(v => v.Id),
                "recent" => query.OrderByDescending(v => v.LastAttendanceAt).ThenByDescending(v => v.Id),
                _ => query.OrderByDescending(v => v.Points).ThenByDescending(v => v.Id)
            };

            var items = await query.Select(v => new ViewerPointResponseDto(
                v.Id,
                v.Nickname,
                v.Points,
                v.AttendanceCount,
                v.ConsecutiveAttendanceCount,
                v.LastAttendanceAt
            )).ToPagedListAsync(request.Limit, v => v.Id);

            return Ok(Result<CursorPagedResponse<ViewerPointResponseDto>>.Success(items));
        }

        [HttpGet("{chzzkUid}/donations")]
        public async Task<IActionResult> GetDonations(
            string chzzkUid, 
            [FromQuery] CursorPagedRequest request)
        {
            // [v10.8] 안정성을 위해 서브쿼리 대신 명시적 조인 방식으로 복구하되 AsNoTracking으로 성능 최적화
            var query = context.TableFuncViewerDonations
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(d => d.CoreStreamerProfiles!.ChzzkUid == chzzkUid)
                        .Select(d => new {
                            Id = d.Id,
                            Nickname = d.CoreGlobalViewers!.Nickname,
                            Balance = d.Balance,
                            TotalDonated = d.TotalDonated,
                            UpdatedAt = d.UpdatedAt ?? d.CreatedAt
                        });

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v => v.Nickname.Contains(request.Search));
            }

            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                query = query.Where(v => v.Id < request.Cursor.Value);
            }

            query = request.Sort switch
            {
                "balance" => query.OrderByDescending(v => v.Balance).ThenByDescending(v => v.Id),
                "recent" => query.OrderByDescending(v => v.UpdatedAt).ThenByDescending(v => v.Id),
                _ => query.OrderByDescending(v => v.TotalDonated).ThenByDescending(v => v.Id)
            };

            var items = await query.Select(d => new ViewerDonationResponseDto(
                d.Id,
                d.Nickname,
                d.Balance,
                d.TotalDonated,
                d.UpdatedAt
            )).ToPagedListAsync(request.Limit, d => d.Id);

            return Ok(Result<CursorPagedResponse<ViewerDonationResponseDto>>.Success(items));
        }

        private async Task<CoreStreamerProfiles?> GetCachedProfileAsync(string uid)
        {
            var profile = await identityCache.GetStreamerProfileAsync(uid);
            if (profile != null) return profile;

            return await context.TableCoreStreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == uid);
        }
    }

    public record ViewerPointResponseDto(long Id, string Nickname, long Points, int AttendanceCount, int ConsecutiveAttendanceCount, KstClock? LastAttendanceAt);
    public record ViewerDonationResponseDto(long Id, string Nickname, int Balance, long TotalDonated, KstClock? UpdatedAt);

    public class ChatPointSettingsDto
    {
        public int PointPerChat { get; set; }
        
        public int PointPerDonation1000 { get; set; }

        public int PointPerAttendance { get; set; }

        public bool IsAutoAccumulateDonation { get; set; }
    }
}
