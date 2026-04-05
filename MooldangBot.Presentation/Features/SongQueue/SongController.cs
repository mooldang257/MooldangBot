using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.Configuration; // [Phase 1] 설정 연동

using MooldangBot.Application.Common.Models; // [v6.3.1] 결과 규격 통일
using MooldangBot.Application.Common.Helpers; // [v12.5] 메타데이터 키 생성기

namespace MooldangBot.Presentation.Features.SongQueue
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    public class SongController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IOverlayNotificationService _notificationService;
        private readonly IUserSession _userSession;
        private readonly IConfiguration _config;
        private readonly ISongLibraryService _libraryService; // [v13.1] 중앙 병기창 서비스 주입

        public SongController(
            IAppDbContext db, 
            IOverlayNotificationService notificationService,
            IUserSession userSession,
            IConfiguration config,
            ISongLibraryService libraryService)
        {
            _db = db;
            _notificationService = notificationService;
            _userSession = userSession;
            _config = config;
            _libraryService = libraryService;
        }

        /// <summary>
        /// [v2.0] 곡 대기열 목록 조회 (커서 기반 페이지네이션)
        /// </summary>
        [HttpGet("/api/song/queue/{chzzkUid}")]
        public async Task<IResult> GetSongQueue(
            string chzzkUid, 
            [FromQuery] SongStatus? status,
            [FromQuery] int? cursor,
            [FromQuery] int? limit)
        {
            var request = new CursorPagedRequest(cursor, limit ?? 20);
            var targetUid = chzzkUid.ToLower();
            // 🛡️ 보안: 세션 기반 권한 검증 및 정문화된 ID 조회
            var streamer = await _db.StreamerProfiles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);

            if (streamer == null) return Results.Json(Result<string>.Failure("스트리머를 찾을 수 없습니다."), statusCode: 404);

            var streamerId = streamer.Id;
            int maxLimit = (status == SongStatus.Completed) 
                ? 50 
                : _config.GetValue<int>("Pagination:MaxLimit", 100);
            int effectiveLimit = Math.Min(request.Limit, maxLimit);

            // 🔍 기본 쿼리 빌드 (AsNoTracking 성능 최적화)
            var query = _db.SongQueues
                .AsNoTracking()
                .Include(s => s.GlobalViewer)
                .Where(s => s.StreamerProfileId == streamerId);

            // 상태 필터 적용
            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            // 🚀 커서 기반 필터링 (최신순/ID 역순 기준)
            if (request.Cursor.HasValue)
            {
                query = query.Where(s => s.Id < request.Cursor.Value);
            }

            var items = await query
                .OrderByDescending(s => s.Id)
                .Take(effectiveLimit + 1)
                .Select(s => new SongQueueViewDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Artist = s.Artist ?? string.Empty,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    GlobalViewer = s.GlobalViewer,
                    // [v13.1] 통합 정보 조회 (Staging -> Streamer -> Master 순의 COALESCE)
                    Url = _db.MasterSongStagings
                            .Where(l => l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.YoutubeUrl)
                            .FirstOrDefault()
                        ?? _db.StreamerSongLibraries
                            .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.YoutubeUrl)
                            .FirstOrDefault()
                        ?? _db.MasterSongLibraries
                            .Where(m => m.SongLibraryId == s.SongLibraryId)
                            .Select(m => m.YoutubeUrl)
                            .FirstOrDefault(),
                    Lyrics = _db.MasterSongStagings
                            .Where(l => l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.Lyrics)
                            .FirstOrDefault()
                        ?? _db.StreamerSongLibraries
                            .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                            .Select(l => l.Lyrics)
                            .FirstOrDefault()
                        ?? _db.MasterSongLibraries
                            .Where(m => m.SongLibraryId == s.SongLibraryId)
                            .Select(m => m.Lyrics)
                            .FirstOrDefault()
                })
                .ToListAsync();

            bool hasNext = items.Count > effectiveLimit;
            if (hasNext) items.RemoveAt(effectiveLimit);

            int? nextCursor = items.LastOrDefault()?.Id;

            var response = new CursorPagedResponse<SongQueueViewDto>(items, nextCursor, hasNext);
            return Results.Ok(Result<CursorPagedResponse<SongQueueViewDto>>.Success(response));
        }

        [HttpPost("/api/song/add/{chzzkUid}")]
        public async Task<IResult> AddSong(string chzzkUid, [FromBody] SongAddRequest request, [FromQuery] int? omakaseId = null)
        {
            var targetUid = chzzkUid.ToLower();
            var profile = await _db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
                
            if (profile == null) return Results.Json(Result<string>.Failure("스트리머를 찾을 수 없습니다."), statusCode: 404);
            
            var chzzkUidLower = chzzkUid.ToLower();

            // 1. [v13.1] 스테이징 캡처 수행 (신규 생성되거나 기존 ID 반환됨)
            // 서비스 레이어에 책임을 위임하여 중복 생성 방지 및 초성 데이터 보장 (Idempotent)
            var songLibraryId = await _libraryService.CaptureStagingAsync(new SongLibraryCaptureDto
            {
                Title = request.Title,
                Artist = request.Artist ?? "Unknown",
                YoutubeUrl = request.Url ?? string.Empty,
                YoutubeTitle = request.Title,
                Lyrics = request.Lyrics,
                SourceType = (int)MetadataSourceType.Viewer,
                SourceId = request.GlobalViewerId?.ToString()
            });

            // 2. 신청곡 대기열 추가 (발급받은 유일 식별자 사용)
            var newSong = new MooldangBot.Domain.Entities.SongQueue
            {
                StreamerProfileId = profile.Id,
                GlobalViewerId = request.GlobalViewerId,
                Title = request.Title,
                Artist = request.Artist,
                SongLibraryId = songLibraryId, 
                Status = SongStatus.Pending,
                CreatedAt = KstClock.Now
            };
            
            _db.SongQueues.Add(newSong);

            if (omakaseId.HasValue)
            {
                var omakase = await _db.StreamerOmakases
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == omakaseId.Value && o.StreamerProfileId == profile.Id);
                    
                if (omakase != null)
                {
                    omakase.Count--;
                    if (omakase.Count < 0) omakase.Count = 0;

                    var activeSession = await _db.SonglistSessions
                        .IgnoreQueryFilters()
                        .Include(s => s.StreamerProfile)
                        .Where(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.IsActive)
                        .FirstOrDefaultAsync();
                    if (activeSession != null)
                    {
                        activeSession.RequestCount++;
                    }
                }
            }

            await _db.SaveChangesAsync();
            await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
            return Results.Ok(Result<MooldangBot.Domain.Entities.SongQueue>.Success(newSong));
        }

        [HttpPut("/api/song/{chzzkUid}/{id}/status")]
        public async Task<IResult> UpdateStatus(string chzzkUid, int id, [FromQuery] SongStatus status)
        {
            var targetUid = chzzkUid.ToLower();
            var song = await _db.SongQueues
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid);
            if (song != null)
            {
                var activeSession = await _db.SonglistSessions
                    .IgnoreQueryFilters()
                    .Include(s => s.StreamerProfile)
                    .Where(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.IsActive)
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
                    var current = await _db.SongQueues
                        .IgnoreQueryFilters()
                        .Include(s => s.StreamerProfile)
                        .FirstOrDefaultAsync(s => s.StreamerProfile!.ChzzkUid.ToLower() == targetUid && s.Status == SongStatus.Playing);
                    if (current != null)
                    {
                        current.Status = SongStatus.Completed;
                        if (activeSession != null) activeSession.CompleteCount++;
                    }
                }
                song.Status = status;
                await _db.SaveChangesAsync();

                await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
                return Results.Ok(Result<bool>.Success(true));
            }
            return Results.Json(Result<string>.Failure("수정할 곡을 찾을 수 없습니다."), statusCode: 404);
        }

        [HttpPost("/api/song/delete/{chzzkUid}")]
        public async Task<IResult> DeleteSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var targetUid = chzzkUid.ToLower();
            var songs = await _db.SongQueues
                .Include(s => s.StreamerProfile)
                .Where(s => ids.Contains(s.Id) && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();
                
            if (songs.Any())
            {
                _db.SongQueues.RemoveRange(songs);
                await _db.SaveChangesAsync();

                await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
                return Results.Ok(Result<bool>.Success(true));
            }
            return Results.Json(Result<string>.Failure("삭제할 항목을 찾을 수 없습니다."), statusCode: 404);
        }

        [HttpPut("/api/song/{chzzkUid}/{id:int}/edit")]
        public async Task<IResult> UpdateSongDetails(string chzzkUid, int id, [FromBody] SongUpdateRequest request)
        {
            var targetUid = chzzkUid.ToLower();
            var songItem = await _db.SongQueues
                .IgnoreQueryFilters()
                .Include(s => s.StreamerProfile)
                .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfile!.ChzzkUid.ToLower() == targetUid);

            if (songItem == null) return Results.Json(Result<string>.Failure("수정할 곡을 찾을 수 없습니다."), statusCode: 404);

            // [v13.1] 1. 스테이징 테이블 정보 업데이트 및 복구 (서비스 레이어 위임)
            long updatedLibraryId = await _libraryService.UpdateStagingAsync(songItem.SongLibraryId, new SongLibraryCaptureDto
            {
                Title = request.Title ?? songItem.Title,
                Artist = request.Artist ?? songItem.Artist,
                YoutubeUrl = request.Url,
                Lyrics = request.Lyrics,
                SourceType = (int)MetadataSourceType.Streamer, // 수정은 스트리머 권한
                SourceId = "system_edit"
            });

            // 2. 곡 대기열 정보 갱신 및 식별자 동기화 (Auto-Recovery 대응)
            if (!string.IsNullOrWhiteSpace(request.Title)) songItem.Title = request.Title;
            if (request.Artist != null) songItem.Artist = request.Artist;
            
            if (songItem.SongLibraryId != updatedLibraryId)
            {
                songItem.SongLibraryId = updatedLibraryId;
            }

            try
            {
                await _db.SaveChangesAsync();
                await _notificationService.NotifySongQueueChangedAsync(chzzkUid);
                return Results.Ok(Result<MooldangBot.Domain.Entities.SongQueue>.Success(songItem));
            }
            catch (DbUpdateException ex)
            {
                return Results.Json(Result<string>.Failure("데이터베이스 저장 중 오류가 발생했습니다.", ex.Message), statusCode: 500);
            }
        }

    }
}
