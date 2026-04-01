using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Presentation.Features.SongQueue
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    public class SongController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IOverlayNotificationService _notificationService;

        public SongController(IAppDbContext db, IOverlayNotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        [HttpPost("/api/song/add/{chzzkUid}")]
        public async Task<Microsoft.AspNetCore.Http.IResult> AddSong(string chzzkUid, [FromBody] MooldangBot.Domain.Entities.SongQueue newSong, [FromQuery] int? omakaseId = null)
        {
            newSong.Id = 0;
            newSong.ChzzkUid = chzzkUid;
            newSong.CreatedAt = DateTime.UtcNow.AddHours(9); // KST
            _db.SongQueues.Add(newSong);

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
            await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
            return Results.Ok(newSong);
        }

        [HttpPut("/api/song/{chzzkUid}/{id}/status")]
        public async Task<Microsoft.AspNetCore.Http.IResult> UpdateStatus(string chzzkUid, int id, [FromQuery] string status)
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
                .Where(s => ids.Contains(s.Id) && s.ChzzkUid.ToLower() == targetUid)
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
                .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid.ToLower() == targetUid);

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
