using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class SongController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SongController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("/api/song/add")]
        public async Task<IResult> AddSong([FromBody] SongQueue newSong)
        {
            newSong.CreatedAt = DateTime.Now;
            _db.SongQueues.Add(newSong);
            await _db.SaveChangesAsync();
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
            }
            return Results.Ok();
        }

        [HttpPost("/api/song/delete")]
        public async Task<IResult> DeleteSongs([FromBody] List<int> ids)
        {
            var songs = await _db.SongQueues.Where(s => ids.Contains(s.Id)).ToListAsync();
            _db.SongQueues.RemoveRange(songs);
            await _db.SaveChangesAsync();
            return Results.Ok();
        }
    }
}
