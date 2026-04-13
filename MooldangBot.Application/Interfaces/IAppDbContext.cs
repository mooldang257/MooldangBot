using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Contracts.Common.Interfaces
{
    public interface IAppDbContext
    {
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
        
        DbSet<StreamerProfile> StreamerProfiles { get; set; }
        DbSet<SongQueue> SongQueues { get; set; }
        DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
        DbSet<AvatarSetting> AvatarSettings { get; set; }
        DbSet<ChzzkCategory> ChzzkCategories { get; set; }
        DbSet<ChzzkCategoryAlias> ChzzkCategoryAliases { get; set; }
        DbSet<GlobalViewer> GlobalViewers { get; set; }
        DbSet<View_StreamerViewer> StreamerViewers { get; set; }
        DbSet<Roulette> Roulettes { get; set; }
        DbSet<RouletteItem> RouletteItems { get; set; }
        DbSet<PeriodicMessage> PeriodicMessages { get; set; }
        DbSet<SonglistSession> SonglistSessions { get; set; }
        DbSet<OverlayPreset> OverlayPresets { get; set; }
        DbSet<StreamerPreference> StreamerPreferences { get; set; }
        DbSet<BroadcastSession> BroadcastSessions { get; set; }
        DbSet<BroadcastHistoryLog> BroadcastHistoryLogs { get; set; }
        DbSet<UnifiedCommand> UnifiedCommands { get; set; }
        
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
        DbSet<Master_SongLibrary> MasterSongLibraries { get; set; }
        DbSet<Streamer_SongLibrary> StreamerSongLibraries { get; set; } // [v12.5] 스트리머 전용 라이브러리
        DbSet<Master_SongStaging> MasterSongStagings { get; set; }
        DbSet<RouletteLog> RouletteLogs { get; set; }
        DbSet<RouletteSpin> RouletteSpins { get; set; } // [v1.9.9] 룰렛 영속성 레이어 추가
        
        // [v11.1] 천상의 장부 (Celestial Ledger)
        DbSet<PointTransactionHistory> PointTransactionHistories { get; set; }
        DbSet<PointDailySummary> PointDailySummaries { get; set; }
        DbSet<RouletteStatsAggregated> RouletteStatsAggregated { get; set; }
        DbSet<CommandExecutionLog> CommandExecutionLogs { get; set; }
        DbSet<ChatInteractionLog> ChatInteractionLogs { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
