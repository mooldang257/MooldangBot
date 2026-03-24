using Microsoft.AspNetCore.Mvc;
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
                var omakase = await _db.StreamerOmakases
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == omakaseId.Value && o.ChzzkUid == chzzkUid);
                    
                if (omakase != null)
                {
                    omakase.Count--;
                    if (omakase.Count < 0) omakase.Count = 0;

                    var activeSession = await _db.SonglistSessions
                        .IgnoreQueryFilters()
                        .Where(s => s.ChzzkUid == chzzkUid && s.IsActive)
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
            var song = await _db.SongQueues
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid == chzzkUid);
                
            if (song != null)
            {
                var activeSession = await _db.SonglistSessions
                    .IgnoreQueryFilters()
                    .Where(s => s.ChzzkUid == chzzkUid && s.IsActive)
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
                        .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid && s.Status == "Playing");
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
            var songs = await _db.SongQueues
                .IgnoreQueryFilters()
                .Where(s => ids.Contains(s.Id) && s.ChzzkUid == chzzkUid)
                .ToListAsync();
                
            if (songs.Any())
            {
                _db.SongQueues.RemoveRange(songs);
                await _db.SaveChangesAsync();

                await NotifyOverlayAsync(chzzkUid);
            }
            return Results.Ok();
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
