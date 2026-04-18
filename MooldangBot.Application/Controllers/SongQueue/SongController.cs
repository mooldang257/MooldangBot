using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Application.Common.Helpers;
using MooldangBot.Application.Common.Interfaces;

namespace MooldangBot.Application.Controllers.SongQueue
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor ?�용
    public class SongController(
        IAppDbContext db, 
        IOverlayNotificationService notificationService,
        IConfiguration config,
        ISongLibraryService libraryService) : ControllerBase
    {
        /// <summary>
        /// [v2.0] �??�기열 목록 조회 (커서 기반 ?�이지?�이??
        /// </summary>
        [HttpGet("/api/song/queue/{chzzkUid}")]
        public async Task<IActionResult> GetSongQueue(
            string chzzkUid, 
            [FromQuery] SongStatus? status,
            [FromQuery] int? cursor,
            [FromQuery] int? limit)
        {
            var request = new CursorPagedRequest(cursor, limit ?? 20);
            var targetUid = chzzkUid.ToLower();
            
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted);

            if (streamer == null) 
                return NotFound(Result<string>.Failure("?�트리머�?찾을 ???�습?�다."));

            var streamerId = streamer.Id;
            int maxLimit = (status == SongStatus.Completed) 
                ? 50 
                : config.GetValue<int>("Pagination:MaxLimit", 100);
            int effectiveLimit = Math.Min(request.Limit, maxLimit);

            var query = db.SongQueues
                .AsNoTracking()
                .Include(s => s.GlobalViewer)
                .Where(s => s.StreamerProfileId == streamerId && !s.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (request.Cursor.HasValue)
            {
                // [물멍]: ?�기열(Pending)?� ?�청???�름차순)?��?�?커서보다 ??ID�?찾고, ?�머지??최신???�림차순)?��?�??��? ID�?찾습?�다.
                if (status == SongStatus.Pending)
                    query = query.Where(s => s.Id > request.Cursor.Value);
                else
                    query = query.Where(s => s.Id < request.Cursor.Value);
            }

            var items = await (status == SongStatus.Pending 
                ? query.OrderBy(s => s.Id) 
                : query.OrderByDescending(s => s.Id))
                .Take(effectiveLimit + 1)
                .Select(s => new SongQueueViewDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Artist = s.Artist ?? string.Empty,
                    Status = s.Status,
                    Cost = s.Cost,
                    CostType = s.CostType,
                    CreatedAt = s.CreatedAt,
                    GlobalViewer = s.GlobalViewer,
                    Requester = s.RequesterNickname ?? (s.GlobalViewer != null ? s.GlobalViewer.Nickname : "?�명"),
                    Url = db.MasterSongStagings
                            .Where(l => l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.YoutubeUrl)
                            .FirstOrDefault()
                        ?? db.StreamerSongLibraries
                            .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.YoutubeUrl)
                            .FirstOrDefault()
                        ?? db.MasterSongLibraries
                            .Where(m => m.SongLibraryId == s.SongLibraryId)
                            .Select(m => m.YoutubeUrl)
                            .FirstOrDefault(),
                    Lyrics = db.MasterSongStagings
                            .Where(l => l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.Lyrics)
                            .FirstOrDefault()
                        ?? db.StreamerSongLibraries
                            .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.Lyrics)
                            .FirstOrDefault()
                        ?? db.MasterSongLibraries
                            .Where(m => m.SongLibraryId == s.SongLibraryId)
                            .Select(m => m.Lyrics)
                            .FirstOrDefault()
                })
                .ToListAsync();

            bool hasNext = items.Count > effectiveLimit;
            if (hasNext) items.RemoveAt(effectiveLimit);

            int? nextCursor = items.LastOrDefault()?.Id;

            var response = new CursorPagedResponse<SongQueueViewDto>(items, nextCursor, hasNext);
            return Ok(Result<CursorPagedResponse<SongQueueViewDto>>.Success(response));
        }

        [HttpPost("/api/song/add/{chzzkUid}")]
        public async Task<IActionResult> AddSong(string chzzkUid, [FromBody] SongAddRequest request, [FromQuery] int? omakaseId = null)
        {
            var targetUid = chzzkUid.ToLower();
            var profile = await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted);
                
            if (profile == null) 
                return NotFound(Result<string>.Failure("?�트리머�?찾을 ???�습?�다."));
            
            var songLibraryId = await libraryService.CaptureStagingAsync(new SongLibraryCaptureDto
            {
                Title = request.Title,
                Artist = request.Artist ?? "Unknown",
                YoutubeUrl = request.Url ?? string.Empty,
                YoutubeTitle = request.Title,
                Lyrics = request.Lyrics,
                SourceType = (int)MetadataSourceType.Viewer,
                SourceId = request.GlobalViewerId?.ToString()
            });

            var newSong = new MooldangBot.Domain.Entities.SongQueue
            {
                StreamerProfileId = profile.Id,
                GlobalViewerId = request.GlobalViewerId,
                Title = request.Title,
                Artist = request.Artist,
                SongLibraryId = songLibraryId, 
                Status = SongStatus.Pending,
                Cost = request.Cost,
                CostType = request.CostType,
                RequesterNickname = request.RequesterNickname, // [물멍] ?�동 추�? ???�달???�네???�??
                CreatedAt = KstClock.Now
            };
            
            db.SongQueues.Add(newSong);

            if (omakaseId.HasValue)
            {
                var omakase = await db.StreamerOmakases
                    .Where(o => o.Id == omakaseId.Value && o.StreamerProfileId == profile.Id)
                    .Where(o => db.UnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
                    .FirstOrDefaultAsync();
                    
                if (omakase != null)
                {
                    omakase.Count--;
                    if (omakase.Count < 0) omakase.Count = 0;

                    var activeSession = await db.SonglistSessions
                        .Include(s => s.StreamerProfile)
                        .Where(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.IsActive && !s.IsDeleted)
                        .FirstOrDefaultAsync();
                    if (activeSession != null)
                    {
                        activeSession.RequestCount++;
                    }
                }
            }

            await db.SaveChangesAsync();
            await notificationService.NotifySongQueueChangedAsync(chzzkUid);
            return Ok(Result<MooldangBot.Domain.Entities.SongQueue>.Success(newSong));
        }

        [HttpPut("/api/song/{chzzkUid}/{id}/status")]
        public async Task<IActionResult> UpdateStatus(string chzzkUid, int id, [FromQuery] SongStatus status)
        {
            var targetUid = chzzkUid.ToLower();
            var song = await db.SongQueues
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && !s.IsDeleted);
            
            if (song == null)
                return NotFound(Result<string>.Failure("?�정??곡을 찾을 ???�습?�다."));

            var activeSession = await db.SonglistSessions
                .Include(s => s.StreamerProfile)
                .Where(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.IsActive && !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (activeSession != null)
            {
                if (status == SongStatus.Completed && song.Status != SongStatus.Completed)
                {
                    activeSession.CompleteCount++;
                }
                else if (song.Status == SongStatus.Completed && status != SongStatus.Completed)
                {
                    activeSession.CompleteCount--;
                    if (activeSession.CompleteCount < 0) activeSession.CompleteCount = 0;
                }
            }

            if (status == SongStatus.Playing)
            {
                var current = await db.SongQueues
                    .Include(s => s.StreamerProfile)
                    .FirstOrDefaultAsync(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.Status == SongStatus.Playing && !s.IsDeleted);
                if (current != null)
                {
                    current.Status = SongStatus.Completed;
                    if (activeSession != null) activeSession.CompleteCount++;
                }
            }
            song.Status = status;
            await db.SaveChangesAsync();

            await notificationService.NotifySongQueueChangedAsync(chzzkUid);
            return Ok(Result<bool>.Success(true));
        }

        [HttpDelete("/api/song/delete/{chzzkUid}")]
        public async Task<IActionResult> DeleteSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var targetUid = chzzkUid.ToLower();
            var songs = await db.SongQueues
                .Include(s => s.StreamerProfile)
                .Where(s => ids.Contains(s.Id) && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && !s.IsDeleted)
                .ToListAsync();
                
            if (songs.Any())
            {
                db.SongQueues.RemoveRange(songs);
                await db.SaveChangesAsync();

                await notificationService.NotifySongQueueChangedAsync(chzzkUid);
                return Ok(Result<bool>.Success(true));
            }
            return NotFound(Result<string>.Failure("??��????��??찾을 ???�습?�다."));
        }

        [HttpPut("/api/song/{chzzkUid}/{id:int}/edit")]
        public async Task<IActionResult> UpdateSongDetails(string chzzkUid, int id, [FromBody] SongUpdateRequest request)
        {
            var targetUid = chzzkUid.ToLower();
            var songItem = await db.SongQueues
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && !s.IsDeleted);

            if (songItem == null) 
                return NotFound(Result<string>.Failure("?�정??곡을 찾을 ???�습?�다."));

            long updatedLibraryId = await libraryService.UpdateStagingAsync(songItem.SongLibraryId, new SongLibraryCaptureDto
            {
                Title = request.Title ?? songItem.Title,
                Artist = request.Artist ?? songItem.Artist ?? "Unknown",
                YoutubeUrl = request.Url ?? string.Empty,
                Lyrics = request.Lyrics,
                SourceType = (int)MetadataSourceType.Streamer,
                SourceId = "system_edit"
            });

            if (!string.IsNullOrWhiteSpace(request.Title)) songItem.Title = request.Title;
            if (request.Artist != null) songItem.Artist = request.Artist;
            
            if (songItem.SongLibraryId != updatedLibraryId)
            {
                songItem.SongLibraryId = updatedLibraryId;
            }

            try
            {
                await db.SaveChangesAsync();
                await notificationService.NotifySongQueueChangedAsync(chzzkUid);
                return Ok(Result<MooldangBot.Domain.Entities.SongQueue>.Success(songItem));
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, Result<string>.Failure("?�이?�베?�스 ?�??�??�류가 발생?�습?�다.", ex.Message));
            }
        }
        [HttpDelete("/api/song/clear/{chzzkUid}/{status}")]
        public async Task<IActionResult> ClearSongsByStatus(string chzzkUid, SongStatus status)
        {
            var targetUid = chzzkUid.ToLower();
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted);

            if (streamer == null)
                return NotFound(Result<string>.Failure("?�트리머�?찾을 ???�습?�다."));

            // [v10.1] EF Core 7+ ExecuteDeleteAsync�??�용?�여 ?�????�� 최적??
            int deletedCount = await db.SongQueues
                .Where(s => s.StreamerProfileId == streamer.Id && s.Status == status && !s.IsDeleted)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                await notificationService.NotifySongQueueChangedAsync(chzzkUid);
                return Ok(Result<int>.Success(deletedCount));
            }

            return Ok(Result<string>.Success("??��???�역???�습?�다."));
        }
    }
}
