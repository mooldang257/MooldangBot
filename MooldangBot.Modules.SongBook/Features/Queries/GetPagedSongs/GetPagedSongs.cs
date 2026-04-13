using MediatR;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Modules.SongBookModule.Persistence;

namespace MooldangBot.Modules.SongBookModule.Features.Queries.GetPagedSongs;

/// <summary>
/// [오르페우스의 투시]: 페이징 처리된 곡 목록을 조회하는 쿼리입니다.
/// </summary>
public record GetPagedSongsQuery(string StreamerChzzkUid, PagedRequest Request) : IRequest<PagedResponse<SongBook>>;

/// <summary>
/// [오르페우스의 응답]: 곡 목록 조회 쿼리를 처리하는 핸들러입니다.
/// </summary>
public class GetPagedSongsHandler(ISongBookRepository repository) : IRequestHandler<GetPagedSongsQuery, PagedResponse<SongBook>>
{
    public async Task<PagedResponse<SongBook>> Handle(GetPagedSongsQuery query, CancellationToken ct)
    {
        return await repository.GetPagedSongsAsync(query.StreamerChzzkUid, query.Request);
    }
}
