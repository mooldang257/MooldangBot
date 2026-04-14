using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Point.Interfaces;

public interface IPointDbContext
{
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    
    DbSet<StreamerProfile> StreamerProfiles { get; set; }
    DbSet<GlobalViewer> GlobalViewers { get; set; }
    
    // [v7.0] Wallet Architecture 분산화 엔티티
    DbSet<ViewerRelation> ViewerRelations { get; set; }
    DbSet<ViewerPoint> ViewerPoints { get; set; }
    DbSet<ViewerDonation> ViewerDonations { get; set; }
    DbSet<ViewerDonationHistory> ViewerDonationHistories { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
