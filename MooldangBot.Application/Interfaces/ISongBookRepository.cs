using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;

namespace MooldangBot.Contracts.Common.Interfaces;

public interface ISongBookRepository
{
    Task<PagedResponse<SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request);
    Task<SongBook?> GetByIdAsync(int id);
}
