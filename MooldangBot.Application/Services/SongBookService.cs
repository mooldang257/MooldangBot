using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Services;

public class SongBookService : ISongBookService
{
    private readonly ISongBookRepository _repository;

    public SongBookService(ISongBookRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request)
    {
        return await _repository.GetPagedSongsAsync(streamerChzzkUid, request);
    }
}
