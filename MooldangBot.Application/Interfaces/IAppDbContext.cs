using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Interfaces
{
    public interface IAppDbContext
    {
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
        
        DbSet<StreamerProfile> StreamerProfiles { get; set; }
        DbSet<SystemSetting> SystemSettings { get; set; }
        DbSet<SongQueue> SongQueues { get; set; }
        DbSet<StreamerCommand> StreamerCommands { get; set; }
        DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
        DbSet<AvatarSetting> AvatarSettings { get; set; }
        DbSet<ChzzkCategory> ChzzkCategories { get; set; }
        DbSet<ChzzkCategoryAlias> ChzzkCategoryAliases { get; set; }
        DbSet<ViewerProfile> ViewerProfiles { get; set; }
        DbSet<Roulette> Roulettes { get; set; }
        DbSet<RouletteItem> RouletteItems { get; set; }
        DbSet<PeriodicMessage> PeriodicMessages { get; set; }
        DbSet<SonglistSession> SonglistSessions { get; set; }
        DbSet<OverlayPreset> OverlayPresets { get; set; }
        DbSet<SharedComponent> SharedComponents { get; set; }
        DbSet<StreamerManager> StreamerManagers { get; set; }
        DbSet<SongBook> SongBooks { get; set; }
        DbSet<RouletteLog> RouletteLogs { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
