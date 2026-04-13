using MooldangBot.Contracts.SongBook.Interfaces;
using MooldangBot.Contracts.Extensions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Modules.SongBookModule.Persistence;

public class SongBookRepository : ISongBookRepository
{
    private readonly ISongBookDbContext _context;

    public SongBookRepository(ISongBookDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<SongBook>> GetPagedSongsAsync(string streamerChzzkUid, PagedRequest request)
    {
        var query = _context.SongBooks
            .AsNoTracking()
            .Include(s => s.StreamerProfile)
            .Where(s => s.StreamerProfile!.ChzzkUid == streamerChzzkUid);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(s => s.Title.Contains(request.Search) || (s.Artist != null && s.Artist.Contains(request.Search)));
        }

        if (request.LastId > 0)
        {
            query = query.Where(s => s.Id < request.LastId);
        }

        return await query
            .OrderByDescending(s => s.Id)
            .ToPagedListAsync(request.PageSize, s => s.Id);
    }

    public async Task<SongBook?> GetByIdAsync(int id)
    {
        return await _context.SongBooks.FindAsync(id);
    }
}
