using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

public interface ISongBookService
{
    Task<PagedResponse<SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request);
}
