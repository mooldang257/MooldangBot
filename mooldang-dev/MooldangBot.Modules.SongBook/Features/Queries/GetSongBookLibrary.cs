using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Abstractions;

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

public class GetSongBookLibraryHandler(
    ISongBookDbContext db,
    ISongBookRepository repository,
    IIdentityCacheService identityCache,
    MooldangBot.Domain.Contracts.AI.Interfaces.ILlmService llmService) : IRequestHandler<GetSongBookLibraryQuery, Result<SongBookLibraryResponseDto>>
{
    public async Task<Result<SongBookLibraryResponseDto>> Handle(GetSongBookLibraryQuery request, CancellationToken ct)
    {
        // [이지스 통합]: UID 뿐만 아니라 슬러그(Slug)로도 조회 가능하게 개선 (v26.0)
        CoreStreamerProfiles? Streamer = null;
        
        // 1. 캐시에서 조회 시도 (UID 기준)
        Streamer = await identityCache.GetStreamerProfileAsync(request.ChzzkUid, ct);
        
        // 2. 실패 시 슬러그로 UID 찾아서 재시도
        if (Streamer == null)
        {
            var ResolvedUid = await identityCache.GetChzzkUidBySlugAsync(request.ChzzkUid, ct);
            if (ResolvedUid != null)
            {
                Streamer = await identityCache.GetStreamerProfileAsync(ResolvedUid, ct);
            }
        }
        
        // 3. 여전히 없으면 DB 직접 조회 (Fallback)
        if (Streamer == null)
        {
             Streamer = await db.TableCoreStreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ChzzkUid == request.ChzzkUid || s.Slug == request.ChzzkUid, ct);
        }
 
        if (Streamer == null) return Result<SongBookLibraryResponseDto>.Failure("스트리머를 찾을 수 없습니다.");
 
        List<MooldangBot.Domain.Entities.FuncSongBooks> SongEntities;

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            // [오시리스의 도서관]: 노래책 검색 시에는 인지 부하를 줄이기 위해 벡터 검색 대신 텍스트 매칭만 사용합니다.
            SongEntities = await repository.SearchPersonalSongBookAsync(Streamer.Id, request.SearchQuery, null, limit: request.Limit);
        }
        else
        {
            SongEntities = await db.TableFuncSongBooks
                .AsNoTracking()
                .Where(s => s.StreamerProfileId == Streamer.Id && !s.IsDeleted && s.IsActive)
                .OrderBy(s => s.Title)
                .Select(s => new MooldangBot.Domain.Entities.FuncSongBooks {
                    Id = s.Id,
                    SongLibraryId = s.SongLibraryId,
                    Title = s.Title,
                    Artist = s.Artist,
                    ReferenceUrl = s.ReferenceUrl,
                    Alias = s.Alias,
                    ThumbnailUrl = s.ThumbnailUrl,
                    Category = s.Category,
                    RequiredPoints = s.RequiredPoints
                })
                .Take(request.Limit)
                .ToListAsync(ct);
        }
 
        var Songs = SongEntities.Select(s => new SongBookLibraryDto(
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
            .ToList();
 
        return Result<SongBookLibraryResponseDto>.Success(new SongBookLibraryResponseDto(Streamer.ChannelName ?? string.Empty, Songs));
    }
}
