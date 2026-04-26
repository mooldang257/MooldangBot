using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Extensions;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Application.Common.Helpers;
using MooldangBot.Application.Common.Interfaces;
using NewSongBookState = MooldangBot.Modules.SongBook.State.SongBookState;
using MooldangBot.Modules.SongBook.State;

namespace MooldangBot.Application.Controllers.SongQueue
{
    [ApiController]
    [Route("api/song/{chzzkUid}")]
    [Authorize(Policy = "chzzk-access")]
    // [v10.1] Primary Constructor 적용
    public class SongController(
        IAppDbContext db, 
        IOverlayNotificationService notificationService,
        IConfiguration config,
        ISongLibraryService libraryService,
        IIdentityCacheService identityCache,
        NewSongBookState songBuffer) : ControllerBase
    {
        /// <summary>
        /// 곡 대기열 목록 조회 (커서 기반 페이지네이션)
        /// </summary>
        [HttpGet("queue")]
        public async Task<IActionResult> GetSongQueue(
            string chzzkUid, 
            [FromQuery] SongStatus? status,
            [FromQuery] CursorPagedRequest request)
        {
            var streamer = await GetCachedProfileAsync(chzzkUid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var streamerId = streamer.Id;
            int maxLimit = (status == SongStatus.Completed) 
                ? 50 
                : config.GetValue<int>("Pagination:MaxLimit", 100);
            int effectiveLimit = Math.Min(request.Limit, maxLimit);

            var query = db.FuncSongQueues
                .AsNoTracking()
                .Include(s => s.GlobalViewer)
                .Where(s => s.StreamerProfileId == streamerId && !s.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                // [물멍]: 대기열(Pending)은 신청순(오름차순), 나머지는 최신순(내림차순)으로 커서 조건을 분기합니다.
                if (status == SongStatus.Pending)
                    query = query.Where(s => s.Id > request.Cursor.Value);
                else
                    query = query.Where(s => s.Id < request.Cursor.Value);
            }

            var pagedSource = (status == SongStatus.Pending 
                ? query.OrderBy(s => s.SortOrder).ThenBy(s => s.Id) 
                : query.OrderByDescending(s => s.Id))
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
                    Requester = s.RequesterNickname ?? (s.GlobalViewer != null ? s.GlobalViewer.Nickname : "익명"),
                    Url = db.FuncMasterSongStagings
                            .Where(l => l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.YoutubeUrl)
                            .FirstOrDefault()
                        ?? db.FuncStreamerSongLibraries
                            .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.YoutubeUrl)
                            .FirstOrDefault()
                        ?? db.FuncMasterSongLibraries
                            .Where(m => m.SongLibraryId == s.SongLibraryId)
                            .Select(m => m.YoutubeUrl)
                            .FirstOrDefault(),
                    LyricsUrl = db.FuncMasterSongStagings
                            .Where(l => l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.LyricsUrl)
                            .FirstOrDefault()
                        ?? db.FuncStreamerSongLibraries
                            .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.LyricsUrl)
                            .FirstOrDefault()
                        ?? db.FuncMasterSongLibraries
                            .Where(m => m.SongLibraryId == s.SongLibraryId)
                            .Select(m => m.LyricsUrl)
                            .FirstOrDefault(),
                    ThumbnailUrl = s.ThumbnailUrl
                });

            var pagedResult = await pagedSource.ToPagedListAsync(effectiveLimit, s => s.Id);

            return Ok(Result<CursorPagedResponse<SongQueueViewDto>>.Success(pagedResult));
        }

        [HttpPost]
        public async Task<IActionResult> AddSong(string chzzkUid, [FromBody] SongAddRequest request, [FromQuery] int? omakaseId = null)
        {
            var profile = await GetCachedProfileAsync(chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));
            
            var songLibraryId = await libraryService.CaptureStagingAsync(new SongLibraryCaptureDto
            {
                Title = request.Title,
                Artist = request.Artist ?? "Unknown",
                YoutubeUrl = request.Url ?? string.Empty,
                YoutubeTitle = request.Title,
                LyricsUrl = request.LyricsUrl,
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
                RequesterNickname = request.RequesterNickname, // [물멍] 자동 추가 시 전달된 닉네임 사용
                VideoId = request.Url, // [물멍] URL을 VideoId로 활용
                ThumbnailUrl = request.ThumbnailUrl, // [물멍] 전달된 썸네일 저장
                CreatedAt = KstClock.Now
            };
            
            db.FuncSongQueues.Add(newSong);

            if (omakaseId.HasValue)
            {
                var omakase = await db.FuncStreamerOmakases
                    .Where(o => o.Id == omakaseId.Value && o.StreamerProfileId == profile.Id)
                    .Where(o => db.SysUnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
                    .FirstOrDefaultAsync();
                    
                if (omakase != null)
                {
                    omakase.Count--;
                    if (omakase.Count < 0) omakase.Count = 0;

                    var activeSession = await db.FuncSonglistSessions
                        .Include(s => s.StreamerProfile)
                        .Where(s => s.StreamerProfileId == profile.Id && s.IsActive && !s.IsDeleted)
                        .FirstOrDefaultAsync();
                    if (activeSession != null)
                    {
                        activeSession.RequestCount++;
                    }
                }
            }

            await db.SaveChangesAsync();

            // [MODERN]: 인메모리 버퍼(SongBookState) 즉시 반영
            songBuffer.AddSong(chzzkUid, newSong.Id, newSong.RequesterNickname ?? "익명", newSong.Title, newSong.Artist);

            await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
            
            var responseDto = new SongQueueResponseDto
            {
                Id = newSong.Id,
                StreamerProfileId = newSong.StreamerProfileId,
                GlobalViewerId = newSong.GlobalViewerId ?? 0,
                ViewerNickname = newSong.RequesterNickname ?? "익명",
                Title = newSong.Title,
                Artist = newSong.Artist ?? "Unknown",
                Status = newSong.Status,
                FinalCost = newSong.Cost ?? 0,
                CreatedAt = newSong.CreatedAt,
                IsPriority = false // [물멍]: 현재 엔티티에 IsPriority 필드 부재로 false 고정
            };

            return Ok(Result<SongQueueResponseDto>.Success(responseDto));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string chzzkUid, int id, [FromQuery] SongStatus status)
        {
            var profile = await GetCachedProfileAsync(chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var song = await db.FuncSongQueues
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);
            
            if (song == null)
                return NotFound(Result<string>.Failure("지정된 곡을 찾을 수 없습니다."));

            var activeSession = await db.FuncSonglistSessions
                .Where(s => s.StreamerProfileId == profile.Id && s.IsActive && !s.IsDeleted)
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
                var current = await db.FuncSongQueues
                    .FirstOrDefaultAsync(s => s.StreamerProfileId == profile.Id && s.Status == SongStatus.Playing && !s.IsDeleted);
                if (current != null)
                {
                    current.Status = SongStatus.Completed;
                    if (activeSession != null) activeSession.CompleteCount++;
                }
            }
            song.Status = status;
            await db.SaveChangesAsync();

            // [MODERN]: 인메모리 버퍼(SongBookState) 상태 동기화
            if (status == SongStatus.Playing)
            {
                songBuffer.SetCurrentSong(chzzkUid, song.Id, song.Title, song.Artist ?? "");
                songBuffer.RemoveSong(chzzkUid, song.Id); // 큐에서 재생 중으로 이동 시 큐에서는 제거
            }
            else if (status == SongStatus.Completed || status == SongStatus.Cancelled)
            {
                songBuffer.RemoveSong(chzzkUid, song.Id);
                var current = songBuffer.GetCurrentSong(chzzkUid);
                if (current != null && current.Id == song.Id)
                {
                    songBuffer.ClearCurrentSong(chzzkUid);
                }
            }

            await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
            return Ok(Result<bool>.Success(true));
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var profile = await GetCachedProfileAsync(chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var songs = await db.FuncSongQueues
                .Where(s => ids.Contains(s.Id) && s.StreamerProfileId == profile.Id && !s.IsDeleted)
                .ToListAsync();
                
            if (songs.Any())
            {
                db.FuncSongQueues.RemoveRange(songs);
                await db.SaveChangesAsync();

                // [MODERN]: 인메모리 버퍼(SongBookState) 벌크 동기화
                foreach (var songId in ids)
                {
                    songBuffer.RemoveSong(chzzkUid, songId);
                }

                await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
                return Ok(Result<bool>.Success(true));
            }
            return NotFound(Result<string>.Failure("삭제할 대상을 찾을 수 없습니다."));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSongDetails(string chzzkUid, int id, [FromBody] SongUpdateRequest request)
        {
            var profile = await GetCachedProfileAsync(chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var songItem = await db.FuncSongQueues
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);

            if (songItem == null) 
                return NotFound(Result<string>.Failure("지정된 곡을 찾을 수 없습니다."));

            long updatedLibraryId = await libraryService.UpdateStagingAsync(songItem.SongLibraryId, new SongLibraryCaptureDto
            {
                Title = request.Title ?? songItem.Title,
                Artist = request.Artist ?? songItem.Artist ?? "Unknown",
                YoutubeUrl = request.Url ?? string.Empty,
                LyricsUrl = request.LyricsUrl,
                SourceType = (int)MetadataSourceType.Streamer,
                SourceId = "system_edit"
            });

            if (!string.IsNullOrWhiteSpace(request.Title)) songItem.Title = request.Title;
            if (request.Artist != null) songItem.Artist = request.Artist;
            if (request.ThumbnailUrl != null) songItem.ThumbnailUrl = request.ThumbnailUrl;
            
            if (songItem.SongLibraryId != updatedLibraryId)
            {
                songItem.SongLibraryId = updatedLibraryId;
            }

            try
            {
                await db.SaveChangesAsync();
                await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
                
                var resultDto = new SongQueueResponseDto
                {
                    Id = songItem.Id,
                    StreamerProfileId = songItem.StreamerProfileId,
                    GlobalViewerId = songItem.GlobalViewerId ?? 0,
                    ViewerNickname = songItem.RequesterNickname ?? "익명",
                    Title = songItem.Title,
                    Artist = songItem.Artist ?? "Unknown",
                    Status = songItem.Status,
                    FinalCost = songItem.Cost ?? 0,
                    CreatedAt = songItem.CreatedAt,
                    IsPriority = false // [물멍]: 현재 엔티티에 IsPriority 필드 부재로 false 고정
                };
                
                return Ok(Result<SongQueueResponseDto>.Success(resultDto));
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, Result<string>.Failure("데이터베이스 저장 중 오류가 발생했습니다.", ex.Message));
            }
        }

        [HttpDelete("clear/{status}")]
        public async Task<IActionResult> ClearSongsByStatus(string chzzkUid, SongStatus status)
        {
            var streamer = await GetCachedProfileAsync(chzzkUid);
            if (streamer == null)
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // [v10.1] EF Core 7+ ExecuteDeleteAsync를 사용하여 대량 삭제 최적화
            int deletedCount = await db.FuncSongQueues
                .Where(s => s.StreamerProfileId == streamer.Id && s.Status == status && !s.IsDeleted)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                // [MODERN]: 대량 삭제 시 안전을 위해 해당 스트리머 버퍼 초기화 (다음 조회 시 DB에서 Re-sync)
                songBuffer.Clear(chzzkUid);
                
                await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
                return Ok(Result<int>.Success(deletedCount));
            }

            return Ok(Result<string>.Success("삭제할 내역이 없습니다."));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var profile = await GetCachedProfileAsync(chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var songs = await db.FuncSongQueues
                .Where(s => s.StreamerProfileId == profile.Id && ids.Contains(s.Id) && s.Status == SongStatus.Pending && !s.IsDeleted)
                .ToListAsync();

            if (!songs.Any())
                return Ok(Result<bool>.Success(true));

            // [물멍]: 전달받은 ID 순서대로 SortOrder 업데이트
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                var song = songs.FirstOrDefault(s => s.Id == id);
                if (song != null)
                {
                    song.SortOrder = i + 1;
                }
            }

            await db.SaveChangesAsync();

            // [MODERN]: 인메모리 버퍼(SongBookState) 순서 동기화
            songBuffer.ReorderSongs(chzzkUid, ids);

            await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
            
            return Ok(Result<bool>.Success(true));
        }

        private async Task<StreamerProfile?> GetCachedProfileAsync(string uid)
        {
            var profile = await identityCache.GetStreamerProfileAsync(uid);
            if (profile != null) return profile;

            var target = uid.ToLower();
            return await db.CoreStreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
        }
    }
}
