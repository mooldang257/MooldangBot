using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 신청곡 관리에 채널 매니저 정책 적용
    public class SongController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> _hubContext;

        public SongController(AppDbContext db, Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        [HttpPost("/api/song/add/{chzzkUid}")]
        public async Task<IResult> AddSong(string chzzkUid, [FromBody] SongQueue newSong, [FromQuery] int? omakaseId = null)
        {
            newSong.Id = 0;
            newSong.ChzzkUid = chzzkUid; // 🛡️ 경로상의 UID로 강제 고정
            newSong.CreatedAt = DateTime.Now;
            _db.SongQueues.Add(newSong);

            // --- 오마카세 연동 ---
            if (omakaseId.HasValue)
            {
                var targetUid = chzzkUid.ToLower();
                var omakase = await _db.StreamerOmakases
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == omakaseId.Value && o.ChzzkUid.ToLower() == targetUid);
                    
                if (omakase != null)
                {
                    omakase.Count--;
                    if (omakase.Count < 0) omakase.Count = 0;

                    var activeSession = await _db.SonglistSessions
                        .IgnoreQueryFilters()
                        .Where(s => s.ChzzkUid.ToLower() == targetUid && s.IsActive)
                        .FirstOrDefaultAsync();
                    if (activeSession != null)
                    {
                        activeSession.RequestCount++;
                    }
                }
            }

            await _db.SaveChangesAsync();
            await NotifyOverlayAsync(chzzkUid);
            return Results.Ok(newSong);
        }

        [HttpPut("/api/song/{chzzkUid}/{id}/status")]
        public async Task<IResult> UpdateStatus(string chzzkUid, int id, [FromQuery] string status)
        {
            var targetUid = chzzkUid.ToLower();
            var song = await _db.SongQueues
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid.ToLower() == targetUid);
                
            if (song != null)
            {
                var activeSession = await _db.SonglistSessions
                    .IgnoreQueryFilters()
                    .Where(s => s.ChzzkUid.ToLower() == targetUid && s.IsActive)
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
                        .FirstOrDefaultAsync(s => s.ChzzkUid.ToLower() == targetUid && s.Status == "Playing");
                    if (current != null)
                    {
                        current.Status = "Completed";
                        if (activeSession != null) activeSession.CompleteCount++;
                    }
                }
                song.Status = status;
                await _db.SaveChangesAsync();

                await NotifyOverlayAsync(chzzkUid);
            }
            return Results.Ok();
        }

        [HttpPost("/api/song/delete/{chzzkUid}")]
        public async Task<IResult> DeleteSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var targetUid = chzzkUid.ToLower();
            var songs = await _db.SongQueues
                .IgnoreQueryFilters()
                .Where(s => ids.Contains(s.Id) && s.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();
                
            if (songs.Any())
            {
                _db.SongQueues.RemoveRange(songs);
                await _db.SaveChangesAsync();

                await NotifyOverlayAsync(chzzkUid);
            }
            return Results.Ok();
        }

        /// <summary>
        /// 대기열 내 특정 곡의 정보를 수정합니다. (PUT /api/song/{chzzkUid}/{id}/edit)
        /// </summary>
        [HttpPut("/api/song/{chzzkUid}/{id:int}/edit")]
        public async Task<IActionResult> UpdateSongDetails(string chzzkUid, int id, [FromBody] SongUpdateRequest request)
        {
            var targetUid = chzzkUid.ToLower();
            // 1. 데이터 조회 (해당 스트리머의 대기열에 속한 곡인지 검증)
            var songItem = await _db.SongQueues
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid.ToLower() == targetUid);

            if (songItem == null)
            {
                return NotFound(new { message = "수정할 곡을 찾을 수 없습니다." });
            }

            // 2. 비즈니스 로직: 요청된 필드 업데이트
            // SongBook과의 연동 없이 SongQueue 테이블의 데이터만 직접 수정합니다.
            if (!string.IsNullOrWhiteSpace(request.Title)) songItem.Title = request.Title;
            if (request.Artist != null) songItem.Artist = request.Artist; // 가수 정보는 null 허용이므로 빈 칸 공백 체크보다 직접 대입 (또는 null 체크)
            
            try
            {
                await _db.SaveChangesAsync();
                
                // 3. 실시간 동기화: 백엔드 주도로 오버레이에 즉시 알림 전송
                await NotifyOverlayAsync(chzzkUid);

                return Ok(new { success = true, message = "곡 정보가 성공적으로 수정되었습니다.", data = songItem });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "데이터베이스 저장 중 오류가 발생했습니다.", details = ex.Message });
            }
        }

        private async Task NotifyOverlayAsync(string chzzkUid)
        {
            string groupName = chzzkUid.ToLower();
            // ⭐ [성능 개선 #6] 2번 개별 SignalR 전송 → 1번으로 통합
            // 프론트엔드에서 'RefreshSongAndDashboard' 이벤트 하나로 답장 화면 + 대시보드 갱신 명령어를 모두 처리해야 함
            await _hubContext.Clients.Group(groupName).SendAsync("RefreshSongAndDashboard");
        }
    }
}
