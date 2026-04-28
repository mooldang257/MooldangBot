using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Features.Queries;

/// <summary>
/// [오시리스의 목록]: 특정 스트리머의 노래책 라이브러리를 조회하는 쿼리입니다.
/// </summary>
public record GetSongBookLibraryQuery(string ChzzkUid, string? SearchQuery = null, int Limit = 50) : IRequest<Result<SongBookLibraryResponseDto>>;

public record SongBookLibraryResponseDto(
    string ChannelName,
    List<SongBookLibraryDto> Songs
);

public record SongBookLibraryDto(
    int Id,
    long? SongLibraryId,
    string Title,
    string? Artist,
    string? YoutubeUrl,
    string? Alias,
    string? ThumbnailUrl,
    string? Category,
    int RequiredPoints
);

public class GetSongBookLibraryHandler(ISongBookDbContext db) : IRequestHandler<GetSongBookLibraryQuery, Result<SongBookLibraryResponseDto>>
{
    public async Task<Result<SongBookLibraryResponseDto>> Handle(GetSongBookLibraryQuery request, CancellationToken ct)
    {
        var streamer = await db.CoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ChzzkUid == request.ChzzkUid, ct);

        if (streamer == null) return Result<SongBookLibraryResponseDto>.Failure("스트리머를 찾을 수 없습니다.");

        var query = db.FuncSongBooks
            .AsNoTracking()
            .Where(s => s.StreamerProfileId == streamer.Id && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var search = request.SearchQuery.Trim().ToLower();
            query = query.Where(s => 
                s.Title.ToLower().Contains(search) || 
                (s.Artist != null && s.Artist.ToLower().Contains(search)) ||
                (s.Alias != null && s.Alias.ToLower().Contains(search)) ||
                (s.TitleChosung != null && s.TitleChosung.Contains(search))
            );
        }

        var songs = await query
            .OrderBy(s => s.Title)
            .Take(request.Limit)
            .Select(s => new SongBookLibraryDto(
                s.Id,
                s.SongLibraryId,
                s.Title,
                s.Artist,
                s.ReferenceUrl, 
                s.Alias,
                s.ThumbnailUrl,
                s.Category,
                s.RequiredPoints
            ))
            .ToListAsync(ct);

        return Result<SongBookLibraryResponseDto>.Success(new SongBookLibraryResponseDto(streamer.ChannelName, songs));
    }
}
