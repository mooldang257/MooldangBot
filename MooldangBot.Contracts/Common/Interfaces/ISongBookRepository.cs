using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;

namespace MooldangBot.Contracts.Common.Interfaces;

public interface ISongBookRepository
{
    Task<PagedResponse<MooldangBot.Domain.Entities.SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request);
    Task<MooldangBot.Domain.Entities.SongBook?> GetByIdAsync(int id);
}
