using Microsoft.EntityFrameworkCore;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Persistence;

public class SongBookRepository(ISongBookDbContext db) : ISongBookRepository
{
    public async Task<MooldangBot.Domain.Entities.SongBook?> GetByStreamerIdAsync(string streamerUid)
    {
        return await db.SongBooks
            .FirstOrDefaultAsync(s => s.StreamerProfile != null && s.StreamerProfile.ChzzkUid == streamerUid);
    }

    public async Task AddAsync(MooldangBot.Domain.Entities.SongBook songBook)
    {
        db.SongBooks.Add(songBook);
        await db.SaveChangesAsync(default);
    }
}
