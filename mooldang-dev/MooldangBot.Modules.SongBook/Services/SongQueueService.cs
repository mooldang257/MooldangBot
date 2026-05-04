using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Extensions;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Modules.SongBook.State;

namespace MooldangBot.Modules.SongBook.Services;

public class SongQueueService(
    IAppDbContext db,
    IOverlayNotificationService notificationService,
    IConfiguration config,
    ISongLibraryService libraryService,
    IIdentityCacheService identityCache,
    SongBookState songBuffer) : ISongQueueService
{
    public async Task<CursorPagedResponse<SongQueueViewDto>> GetPagedQueueAsync(string chzzkUid, SongStatus? status, CursorPagedRequest request)
    {
        var streamer = await GetCachedProfileAsync(chzzkUid);
        if (streamer == null) throw new KeyNotFoundException("스트리머를 찾을 수 없습니다.");

        int maxLimit = (status == SongStatus.Completed) ? 50 : config.GetValue<int>("Pagination:MaxLimit", 100);
        int effectiveLimit = Math.Min(request.Limit, maxLimit);

        var query = db.TableFuncSongListQueues
            .AsNoTracking()
            .Include(s => s.CoreGlobalViewers)
            .Where(s => s.StreamerProfileId == streamer.Id && !s.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (request.Cursor.HasValue && request.Cursor.Value > 0)
        {
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
                CoreGlobalViewers = s.CoreGlobalViewers,
                Requester = s.RequesterNickname ?? (s.CoreGlobalViewers != null ? s.CoreGlobalViewers.Nickname : "익명"),
                Url = db.TableFuncSongMasterStaging
                        .Where(l => l.SongLibraryId == s.SongLibraryId)
                        .Select(l => l.YoutubeUrl)
                        .FirstOrDefault()
                    ?? db.TableFuncSongStreamerLibrary
                        .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                        .Select(l => l.YoutubeUrl)
                        .FirstOrDefault()
                    ?? db.TableFuncSongMasterLibrary
                        .Where(m => m.SongLibraryId == s.SongLibraryId)
                        .Select(m => m.YoutubeUrl)
                        .FirstOrDefault(),
                LyricsUrl = db.TableFuncSongMasterStaging
                        .Where(l => l.SongLibraryId == s.SongLibraryId)
                        .Select(l => l.LyricsUrl)
                        .FirstOrDefault()
                    ?? db.TableFuncSongStreamerLibrary
                        .Where(l => l.StreamerProfileId == s.StreamerProfileId && l.SongLibraryId == s.SongLibraryId)
                        .Select(l => l.LyricsUrl)
                        .FirstOrDefault()
                    ?? db.TableFuncSongMasterLibrary
                        .Where(m => m.SongLibraryId == s.SongLibraryId)
                        .Select(m => m.LyricsUrl)
                        .FirstOrDefault(),
                ThumbnailUrl = s.ThumbnailUrl
            });

        return await pagedSource.ToPagedListAsync(effectiveLimit, s => s.Id);
    }

    public async Task<Result<SongQueueResponseDto>> AddSongAsync(string chzzkUid, SongAddRequest request, int? omakaseId = null)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Result<SongQueueResponseDto>.Failure("스트리머를 찾을 수 없습니다.");

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

        var newSong = new MooldangBot.Domain.Entities.FuncSongListQueues
        {
            StreamerProfileId = profile.Id,
            GlobalViewerId = request.GlobalViewerId,
            Title = request.Title,
            Artist = request.Artist,
            SongLibraryId = songLibraryId,
            Status = SongStatus.Pending,
            Cost = request.Cost,
            CostType = request.CostType,
            RequesterNickname = request.RequesterNickname,
            VideoId = request.Url,
            ThumbnailUrl = request.ThumbnailUrl,
            CreatedAt = KstClock.Now
        };

        db.TableFuncSongListQueues.Add(newSong);

        if (omakaseId.HasValue)
        {
            var omakase = await db.TableFuncSongListOmakases
                .Where(o => o.Id == omakaseId.Value && o.StreamerProfileId == profile.Id)
                .FirstOrDefaultAsync();

            if (omakase != null)
            {
                omakase.Count--;
                if (omakase.Count < 0) omakase.Count = 0;

                var activeSession = await db.TableFuncSongListSessions
                    .Where(s => s.StreamerProfileId == profile.Id && s.IsActive && !s.IsDeleted)
                    .FirstOrDefaultAsync();
                if (activeSession != null) activeSession.RequestCount++;
            }
        }

        await db.SaveChangesAsync();
        songBuffer.AddSong(chzzkUid, newSong.Id, newSong.RequesterNickname ?? "익명", newSong.Title, newSong.Artist);
        await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
        await notificationService.NotifySongQueueChangedAsync(chzzkUid);

        return Result<SongQueueResponseDto>.Success(new SongQueueResponseDto
        {
            Id = newSong.Id,
            StreamerProfileId = newSong.StreamerProfileId,
            GlobalViewerId = newSong.GlobalViewerId ?? 0,
            ViewerNickname = newSong.RequesterNickname ?? "익명",
            Title = newSong.Title,
            Artist = newSong.Artist ?? "Unknown",
            Status = newSong.Status,
            FinalCost = newSong.Cost ?? 0,
            CreatedAt = newSong.CreatedAt
        });
    }

    public async Task<Result<bool>> UpdateStatusAsync(string chzzkUid, int id, SongStatus status)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Result<bool>.Failure("스트리머를 찾을 수 없습니다.");

        var song = await db.TableFuncSongListQueues
            .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);

        if (song == null) return Result<bool>.Failure("지정된 곡을 찾을 수 없습니다.");

        var activeSession = await db.TableFuncSongListSessions
            .Where(s => s.StreamerProfileId == profile.Id && s.IsActive && !s.IsDeleted)
            .FirstOrDefaultAsync();

        if (activeSession != null)
        {
            if (status == SongStatus.Completed && song.Status != SongStatus.Completed)
                activeSession.CompleteCount++;
            else if (song.Status == SongStatus.Completed && status != SongStatus.Completed)
            {
                activeSession.CompleteCount--;
                if (activeSession.CompleteCount < 0) activeSession.CompleteCount = 0;
            }
        }

        if (status == SongStatus.Playing)
        {
            var current = await db.TableFuncSongListQueues
                .FirstOrDefaultAsync(s => s.StreamerProfileId == profile.Id && s.Status == SongStatus.Playing && !s.IsDeleted);
            if (current != null)
            {
                current.Status = SongStatus.Completed;
                if (activeSession != null) activeSession.CompleteCount++;
            }
            songBuffer.SetCurrentSong(chzzkUid, song.Id, song.Title, song.Artist ?? "");
            songBuffer.RemoveSong(chzzkUid, song.Id);
        }
        else if (status == SongStatus.Completed || status == SongStatus.Cancelled)
        {
            songBuffer.RemoveSong(chzzkUid, song.Id);
            var current = songBuffer.GetCurrentSong(chzzkUid);
            if (current != null && current.Id == song.Id) songBuffer.ClearCurrentSong(chzzkUid);
        }

        song.Status = status;
        await db.SaveChangesAsync();
        await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
        await notificationService.NotifySongQueueChangedAsync(chzzkUid);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteSongsAsync(string chzzkUid, List<int> ids)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Result<bool>.Failure("스트리머를 찾을 수 없습니다.");

        var songs = await db.TableFuncSongListQueues
            .Where(s => ids.Contains(s.Id) && s.StreamerProfileId == profile.Id && !s.IsDeleted)
            .ToListAsync();

        if (!songs.Any()) return Result<bool>.Failure("삭제할 대상을 찾을 수 없습니다.");

        db.TableFuncSongListQueues.RemoveRange(songs);
        await db.SaveChangesAsync();

        foreach (var songId in ids) songBuffer.RemoveSong(chzzkUid, songId);
        await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
        await notificationService.NotifySongQueueChangedAsync(chzzkUid);
        return Result<bool>.Success(true);
    }

    public async Task<Result<SongQueueResponseDto>> UpdateSongDetailsAsync(string chzzkUid, int id, SongUpdateRequest request)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Result<SongQueueResponseDto>.Failure("스트리머를 찾을 수 없습니다.");

        var songItem = await db.TableFuncSongListQueues
            .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);

        if (songItem == null) return Result<SongQueueResponseDto>.Failure("지정된 곡을 찾을 수 없습니다.");

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
        songItem.SongLibraryId = updatedLibraryId;

        await db.SaveChangesAsync();
        await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
        await notificationService.NotifySongQueueChangedAsync(chzzkUid);

        return Result<SongQueueResponseDto>.Success(new SongQueueResponseDto
        {
            Id = songItem.Id,
            StreamerProfileId = songItem.StreamerProfileId,
            GlobalViewerId = songItem.GlobalViewerId ?? 0,
            ViewerNickname = songItem.RequesterNickname ?? "익명",
            Title = songItem.Title,
            Artist = songItem.Artist ?? "Unknown",
            Status = songItem.Status,
            FinalCost = songItem.Cost ?? 0,
            CreatedAt = songItem.CreatedAt
        });
    }

    public async Task<Result<int>> ClearSongsByStatusAsync(string chzzkUid, SongStatus status)
    {
        var streamer = await GetCachedProfileAsync(chzzkUid);
        if (streamer == null) return Result<int>.Failure("스트리머를 찾을 수 없습니다.");

        int deletedCount = await db.TableFuncSongListQueues
            .Where(s => s.StreamerProfileId == streamer.Id && s.Status == status && !s.IsDeleted)
            .ExecuteDeleteAsync();

        if (deletedCount > 0)
        {
            songBuffer.Clear(chzzkUid);
            await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
            await notificationService.NotifySongQueueChangedAsync(chzzkUid);
            return Result<int>.Success(deletedCount);
        }
        return Result<int>.Success(0);
    }

    public async Task<Result<bool>> ReorderSongsAsync(string chzzkUid, List<int> ids)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Result<bool>.Failure("스트리머를 찾을 수 없습니다.");

        var songs = await db.TableFuncSongListQueues
            .Where(s => s.StreamerProfileId == profile.Id && ids.Contains(s.Id) && s.Status == SongStatus.Pending && !s.IsDeleted)
            .ToListAsync();

        if (!songs.Any()) return Result<bool>.Success(true);

        for (int i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            var song = songs.FirstOrDefault(s => s.Id == id);
            if (song != null) song.SortOrder = i + 1;
        }

        await db.SaveChangesAsync();
        songBuffer.ReorderSongs(chzzkUid, ids);
        await notificationService.BroadcastSongOverlayUpdateAsync(chzzkUid);
        return Result<bool>.Success(true);
    }

    private async Task<CoreStreamerProfiles?> GetCachedProfileAsync(string uid)
    {
        var profile = await identityCache.GetStreamerProfileAsync(uid);
        if (profile != null) return profile;

        var target = uid.ToLower();
        return await db.TableCoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
    }
}
