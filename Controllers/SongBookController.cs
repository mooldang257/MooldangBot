using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;

namespace MooldangAPI.Controllers
{
    public class SongBookPagedResponse
    {
        public List<SongBook> Data { get; set; } = new();
        public int? NextLastId { get; set; }
    }

    [ApiController]
    [Route("api/songbook")]
    [Authorize(Policy = "ChannelManager")]
    public class SongBookController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SongBookController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetSongs(string chzzkUid, [FromQuery] int LastId = 0, [FromQuery] int PageSize = 20, [FromQuery] string? Search = null)
        {
            var query = _db.SongBooks
                .IgnoreQueryFilters()
                .Where(s => s.ChzzkUid == chzzkUid);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(s => s.Title.Contains(Search) || (s.Artist != null && s.Artist.Contains(Search)));
            }

            if (LastId > 0)
            {
                query = query.Where(s => s.Id < LastId);
            }

            var rawData = await query
                .OrderByDescending(s => s.Id)
                .Take(PageSize + 1)
                .AsNoTracking()
                .ToListAsync();

            var hasNext = rawData.Count > PageSize;
            var outputData = hasNext ? rawData.Take(PageSize).ToList() : rawData;
            int? nextLastId = hasNext ? outputData.Last().Id : null;

            return Ok(new SongBookPagedResponse
            {
                Data = outputData,
                NextLastId = nextLastId
            });
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> AddSong(string chzzkUid, [FromBody] SongBook song)
        {
            song.Id = 0;
            song.ChzzkUid = chzzkUid;
            song.CreatedAt = DateTime.Now;
            song.UpdatedAt = DateTime.Now;

            _db.SongBooks.Add(song);
            await _db.SaveChangesAsync();

            return Ok(song);
        }

        [HttpPut("{chzzkUid}/{id}")]
        public async Task<IActionResult> UpdateSong(string chzzkUid, int id, [FromBody] SongBook updated)
        {
            var song = await _db.SongBooks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid == chzzkUid);

            if (song == null) return NotFound();

            song.Title = updated.Title;
            song.Artist = updated.Artist;
            song.IsActive = updated.IsActive;
            song.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return Ok(song);
        }

        [HttpDelete("{chzzkUid}/{id}")]
        public async Task<IActionResult> DeleteSong(string chzzkUid, int id)
        {
            var song = await _db.SongBooks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid == chzzkUid);

            if (song == null) return NotFound();

            _db.SongBooks.Remove(song);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{chzzkUid}/add-to-queue/{id}")]
        public async Task<IActionResult> AddToQueue(string chzzkUid, int id)
        {
            var song = await _db.SongBooks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid == chzzkUid);

            if (song == null) return NotFound();

            // 현재 대기열의 마지막 순서 찾기
            var lastOrder = await _db.SongQueues
                .IgnoreQueryFilters()
                .Where(q => q.ChzzkUid == chzzkUid)
                .OrderByDescending(q => q.SortOrder)
                .Select(q => q.SortOrder)
                .FirstOrDefaultAsync();

            var queueItem = new SongQueue
            {
                ChzzkUid = chzzkUid,
                Title = song.Title,
                Artist = song.Artist,
                Status = "Pending",
                SortOrder = lastOrder + 1,
                CreatedAt = DateTime.Now
            };

            _db.SongQueues.Add(queueItem);
            
            // 사용 횟수 증가
            song.UsageCount++;
            
            await _db.SaveChangesAsync();

            return Ok(new { Success = true, QueueItem = queueItem });
        }
    }
}
