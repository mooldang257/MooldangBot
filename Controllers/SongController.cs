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
        public async Task<IResult> AddSong([FromBody] SongQueue newSong)
        {
            newSong.CreatedAt = DateTime.Now;
            _db.SongQueues.Add(newSong);
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
                if (status == "Playing")
                {
                    var current = await _db.SongQueues.FirstOrDefaultAsync(s => s.ChzzkUid == song.ChzzkUid && s.Status == "Playing");
                    if (current != null) current.Status = "Completed";
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
