using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.Configuration; // [Phase 1] 설정 연동

namespace MooldangBot.Presentation.Features.SongQueue
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    public class SongController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IOverlayNotificationService _notificationService;
        private readonly IUserSession _userSession; // [v6.1] 세션 정보 주입
        private readonly IConfiguration _config; // [Phase 1] MaxLimit 정책용

        public SongController(
            IAppDbContext db, 
            IOverlayNotificationService notificationService,
            IUserSession userSession,
            IConfiguration config)
        {
            _db = db;
            _notificationService = notificationService;
            _userSession = userSession;
            _config = config;
        }

        /// <summary>
        /// [v2.0] 곡 대기열 목록 조회 (커서 기반 페이지네이션)
        /// </summary>
        [HttpGet("/api/song/queue/{chzzkUid}")]
        public async Task<IResult> GetSongQueue(
            string chzzkUid, 
            [FromQuery] string? status,
            [AsParameters] CursorPagedRequest request)
        {
            // 🛡️ 보안: 세션 기반 권한 검증 및 정문화된 ID 조회
            var streamer = await _db.StreamerProfiles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (streamer == null) return Results.NotFound("스트리머를 찾을 수 없습니다.");

            var streamerId = streamer.Id;
            int maxLimit = _config.GetValue<int>("Pagination:MaxLimit", 100);
            int effectiveLimit = Math.Min(request.Limit, maxLimit);

            // 🔍 기본 쿼리 빌드 (AsNoTracking 성능 최적화)
            var query = _db.SongQueues
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.StreamerProfileId == streamerId);

            // 상태 필터 적용 (있을 경우 IX_SongQueue_Status_Cursor 활용)
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            // 🚀 커서 기반 필터링 (최신순/ID 역순 기준)
            if (request.Cursor.HasValue)
            {
                query = query.Where(s => s.Id < request.Cursor.Value);
            }

            // [Limit + 1] 개를 조회하여 다음 페이지 존재 여부 확인
            var items = await query
                .OrderByDescending(s => s.Id)
                .Take(effectiveLimit + 1)
                .ToListAsync();

            // 응답 데이터 가공
            bool hasNext = items.Count > effectiveLimit;
            if (hasNext) items.RemoveAt(effectiveLimit);

            int? nextCursor = items.LastOrDefault()?.Id;

            return Results.Ok(new CursorPagedResponse<MooldangBot.Domain.Entities.SongQueue>(items, nextCursor, hasNext));
        }

        [HttpPost("/api/song/add/{chzzkUid}")]
        public async Task<Microsoft.AspNetCore.Http.IResult> AddSong(string chzzkUid, [FromBody] MooldangBot.Domain.Entities.SongQueue newSong, [FromQuery] int? omakaseId = null)
        {
            var targetUid = chzzkUid.ToLower();
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
                
            if (profile == null) return Results.NotFound("스트리머를 찾을 수 없습니다.");

            newSong.Id = 0;
            newSong.StreamerProfileId = profile.Id;
            newSong.CreatedAt = KstClock.Now;
            _db.SongQueues.Add(newSong);

            if (omakaseId.HasValue)
            {
                var omakase = await _db.StreamerOmakases
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == omakaseId.Value && o.StreamerProfileId == profile.Id);
                    
                if (omakase != null)
                {
                    omakase.Count--;
                    if (omakase.Count < 0) omakase.Count = 0;

                    var activeSession = await _db.SonglistSessions
                        .IgnoreQueryFilters()
                        .Include(s => s.StreamerProfile)
                        .Where(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.IsActive)
                        .FirstOrDefaultAsync();
                    if (activeSession != null)
                    {
                        activeSession.RequestCount++;
                    }
                }
            }

            await _db.SaveChangesAsync();
            await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
            return Results.Ok(newSong);
        }

        [HttpPut("/api/song/{chzzkUid}/{id}/status")]
        public async Task<Microsoft.AspNetCore.Http.IResult> UpdateStatus(string chzzkUid, int id, [FromQuery] string status)
        {
            var targetUid = chzzkUid.ToLower();
            var song = await _db.SongQueues
                .IgnoreQueryFilters()
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid);
            if (song != null)
            {
                var activeSession = await _db.SonglistSessions
                    .IgnoreQueryFilters()
                    .Include(s => s.StreamerProfile)
                    .Where(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.IsActive)
                    .FirstOrDefaultAsync();

                if (activeSession != null)
                {
                    if (status == "Completed" && song.Status != "Completed")
                    {
                        activeSession.CompleteCount++;
                    }
                    else if (song.Status == "Completed" && status != "Completed")
                    {
                        activeSession.CompleteCount--;
                        if (activeSession.CompleteCount < 0) activeSession.CompleteCount = 0;
                    }
                }

                if (status == "Playing")
                {
                    var current = await _db.SongQueues
                        .IgnoreQueryFilters()
                        .Include(s => s.StreamerProfile)
                        .FirstOrDefaultAsync(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.Status == "Playing");
                    if (current != null)
                    {
                        current.Status = "Completed";
                        if (activeSession != null) activeSession.CompleteCount++;
                    }
                }
                song.Status = status;
                await _db.SaveChangesAsync();

                await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
            }
            return Results.Ok();
        }

        [HttpPost("/api/song/delete/{chzzkUid}")]
        public async Task<Microsoft.AspNetCore.Http.IResult> DeleteSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var targetUid = chzzkUid.ToLower();
            var songs = await _db.SongQueues
                .IgnoreQueryFilters()
                .Include(s => s.StreamerProfile)
                .Where(s => ids.Contains(s.Id) && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();
                
            if (songs.Any())
            {
                _db.SongQueues.RemoveRange(songs);
                await _db.SaveChangesAsync();

                await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
            }
            return Results.Ok();
        }

        [HttpPut("/api/song/{chzzkUid}/{id:int}/edit")]
        public async Task<IActionResult> UpdateSongDetails(string chzzkUid, int id, [FromBody] SongUpdateRequest request)
        {
            var targetUid = chzzkUid.ToLower();
            var songItem = await _db.SongQueues
                .IgnoreQueryFilters()
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid);

            if (songItem == null)
            {
                return NotFound(new { message = "수정할 곡을 찾을 수 없습니다." });
            }

            if (!string.IsNullOrWhiteSpace(request.Title)) songItem.Title = request.Title;
            if (request.Artist != null) songItem.Artist = request.Artist;
            
            try
            {
                await _db.SaveChangesAsync();
                await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
                return Ok(new { success = true, message = "곡 정보가 성공적으로 수정되었습니다.", data = songItem });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "데이터베이스 저장 중 오류가 발생했습니다.", details = ex.Message });
            }
        }
    }
}
