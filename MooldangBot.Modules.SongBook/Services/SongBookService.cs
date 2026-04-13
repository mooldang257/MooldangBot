using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.SongBookModule.Persistence;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.SongBookModule.Services;

public class SongBookService : ISongBookService
{
    private readonly ISongBookRepository _repository;
    private readonly ILogger<SongBookService> _logger;

    public SongBookService(ISongBookRepository repository, ILogger<SongBookService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResponse<SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request)
    {
        return await _repository.GetPagedSongsAsync(streamerChzzkUid, request);
    }

    public async Task<SongBook?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
