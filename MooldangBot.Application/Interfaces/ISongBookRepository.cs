using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Interfaces;

public interface ISongBookRepository
{
    Task<PagedResponse<SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request);
    Task<SongBook?> GetByIdAsync(int id);
}
