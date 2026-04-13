using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Interfaces;

public interface ISongBookDbContext
{
    DbSet<SongBook> SongBooks { get; }
    DbSet<SongQueue> SongQueues { get; }
    DbSet<SonglistSession> SonglistSessions { get; }
    DbSet<GlobalViewer> GlobalViewers { get; }
    DbSet<StreamerProfile> StreamerProfiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
