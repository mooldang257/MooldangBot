using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Infrastructure.Persistence;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/songbook")]
    public class SongBookController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SongBookService _songService;

        public SongBookController(AppDbContext db, SongBookService songService)
        {
            _db = db;
            _songService = songService;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetSongs(string chzzkUid, [FromQuery] int LastId = 0, [FromQuery] int PageSize = 20, [FromQuery] string? Search = null)
        {
            var request = new PagedRequest(LastId, PageSize, Search);
            var result = await _songService.GetPagedSongsAsync(chzzkUid, request);
            
            return Ok(result);
        }

        [Authorize(Policy = "ChannelManager")]
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

        [Authorize(Policy = "ChannelManager")]
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

        [Authorize(Policy = "ChannelManager")]
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

        [Authorize(Policy = "ChannelManager")]
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
