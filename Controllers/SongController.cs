using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class SongController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> _hubContext;

        public SongController(AppDbContext db, Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        [HttpPost("/api/song/add")]
        public async Task<IResult> AddSong([FromBody] SongQueue newSong, [FromQuery] int? omakaseId = null)
        {
            newSong.CreatedAt = DateTime.Now;
            _db.SongQueues.Add(newSong);

            // --- 오마카세 연동 (수동 추가 시 카운트 차감 및 통계 반영) ---
            if (omakaseId.HasValue)
            {
                var omakase = await _db.StreamerOmakases.FindAsync(omakaseId.Value);
                if (omakase != null)
                {
                    omakase.Count--;
                    if (omakase.Count < 0) omakase.Count = 0;

                    // 활성 세션 통계에도 반영
                    var activeSession = await _db.SonglistSessions
                        .Where(s => s.ChzzkUid == newSong.ChzzkUid && s.IsActive)
                        .FirstOrDefaultAsync();
                    if (activeSession != null)
                    {
                        activeSession.RequestCount++;
                    }
                }
            }
            // --------------------------------------------------------

            await _db.SaveChangesAsync();

            // 실시간 갱신 신호 발송
            await NotifyOverlayAsync(newSong.ChzzkUid!);

            return Results.Ok(newSong);
        }

        [HttpPut("/api/song/{id}/status")]
        public async Task<IResult> UpdateStatus(int id, [FromQuery] string status)
        {
            var song = await _db.SongQueues.FindAsync(id);
            if (song != null)
            {
                // --- 통계 기록 (세션 카운트) ---
                var activeSession = await _db.SonglistSessions
                    .Where(s => s.ChzzkUid == song.ChzzkUid && s.IsActive)
                    .FirstOrDefaultAsync();

                if (activeSession != null)
                {
                    // 완료로 변경될 때 +1
                    if (status == "Completed" && song.Status != "Completed")
                    {
                        activeSession.CompleteCount++;
                    }
                    // 완료에서 다른 상태(대기/재생)로 돌아갈 때 -1
                    else if (song.Status == "Completed" && status != "Completed")
                    {
                        activeSession.CompleteCount--;
                        if (activeSession.CompleteCount < 0) activeSession.CompleteCount = 0;
                    }
                }
                // -----------------------------

                if (status == "Playing")
                {
                    var current = await _db.SongQueues.FirstOrDefaultAsync(s => s.ChzzkUid == song.ChzzkUid && s.Status == "Playing");
                    if (current != null)
                    {
                        // 기존 재생 중인 곡을 완료로 보낼 때도 카운트 증가
                        current.Status = "Completed";
                        if (activeSession != null) activeSession.CompleteCount++;
                    }
                }
                song.Status = status;
                await _db.SaveChangesAsync();

                // 실시간 갱신 신호 발송
                await NotifyOverlayAsync(song.ChzzkUid!);
            }
            return Results.Ok();
        }

        [HttpPost("/api/song/delete")]
        public async Task<IResult> DeleteSongs([FromBody] List<int> ids)
        {
            var songs = await _db.SongQueues.Where(s => ids.Contains(s.Id)).ToListAsync();
            if (songs.Any())
            {
                string uid = songs.First().ChzzkUid!;
                _db.SongQueues.RemoveRange(songs);
                await _db.SaveChangesAsync();

                // 실시간 갱신 신호 발송
                await NotifyOverlayAsync(uid);
            }
            return Results.Ok();
        }

        private async Task NotifyOverlayAsync(string chzzkUid)
        {
            string groupName = chzzkUid.ToLower();
            await _hubContext.Clients.Group(groupName).SendAsync("RefreshSonglist");
            await _hubContext.Clients.Group(groupName).SendAsync("RefreshDashboard");
        }
    }
}
