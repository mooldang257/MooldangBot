using Microsoft.EntityFrameworkCore;
using MooldangBot.Modules.SongBookModule.Abstractions;
using MooldangBot.Contracts.SongBook.Interfaces;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBookModule.Persistence;

public class SongBookRepository(ISongBookDbContext db) : ISongBookRepository
{
    public async Task<SongBook?> GetByStreamerIdAsync(string streamerUid)
    {
        return await db.SongBooks
            .FirstOrDefaultAsync(s => s.StreamerProfile != null && s.StreamerProfile.ChzzkUid == streamerUid);
    }

    public async Task AddAsync(SongBook songBook)
    {
        db.SongBooks.Add(songBook);
        await db.SaveChangesAsync(default);
    }
}
