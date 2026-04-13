using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Interfaces;

public interface IPointDbContext
{
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    
    DbSet<StreamerProfile> StreamerProfiles { get; set; }
    DbSet<GlobalViewer> GlobalViewers { get; set; }
    DbSet<View_StreamerViewer> StreamerViewers { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
