using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Application.Interfaces
{
    public interface IAppDbContext
    {
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
        
        DbSet<StreamerProfile> StreamerProfiles { get; set; }
        DbSet<SystemSetting> SystemSettings { get; set; }
        DbSet<SongQueue> SongQueues { get; set; }
        DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
        DbSet<AvatarSetting> AvatarSettings { get; set; }
        DbSet<ChzzkCategory> ChzzkCategories { get; set; }
        DbSet<ChzzkCategoryAlias> ChzzkCategoryAliases { get; set; }
        DbSet<GlobalViewer> GlobalViewers { get; set; }
        DbSet<ViewerProfile> ViewerProfiles { get; set; }
        DbSet<Roulette> Roulettes { get; set; }
        DbSet<RouletteItem> RouletteItems { get; set; }
        DbSet<PeriodicMessage> PeriodicMessages { get; set; }
        DbSet<SonglistSession> SonglistSessions { get; set; }
        DbSet<OverlayPreset> OverlayPresets { get; set; }
        DbSet<BroadcastSession> BroadcastSessions { get; set; }
        DbSet<UnifiedCommand> UnifiedCommands { get; set; }
        DbSet<Master_CommandCategory> MasterCommandCategories { get; set; }
        DbSet<Master_CommandFeature> MasterCommandFeatures { get; set; }
        DbSet<Master_DynamicVariable> MasterDynamicVariables { get; set; }
        
        // IAMF Philosophy
        DbSet<IamfScenario> IamfScenarios { get; set; }
        DbSet<IamfGenosRegistry> IamfGenosRegistries { get; set; }
        DbSet<IamfParhosCycle> IamfParhosCycles { get; set; }
        DbSet<IamfVibrationLog> IamfVibrationLogs { get; set; }
        DbSet<IamfStreamerSetting> IamfStreamerSettings { get; set; }
        DbSet<StreamerKnowledge> StreamerKnowledges { get; set; }

        DbSet<SharedComponent> SharedComponents { get; set; }
        DbSet<StreamerManager> StreamerManagers { get; set; }
        DbSet<SongBook> SongBooks { get; set; }
        DbSet<RouletteLog> RouletteLogs { get; set; }
        DbSet<RouletteSpin> RouletteSpins { get; set; } // [v1.9.9] 룰렛 영속성 레이어 추가

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
