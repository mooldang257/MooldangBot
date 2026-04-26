using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Domain.Abstractions
{
    public interface IAppDbContext
    {
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
        
        DbSet<StreamerProfile> CoreStreamerProfiles { get; set; }
        DbSet<SongQueue> FuncSongQueues { get; set; }
        DbSet<StreamerOmakaseItem> FuncStreamerOmakases { get; set; }
        DbSet<AvatarSetting> SysAvatarSettings { get; set; }
        DbSet<ChzzkCategory> SysChzzkCategories { get; set; }
        DbSet<ChzzkCategoryAlias> SysChzzkCategoryAliases { get; set; }
        DbSet<GlobalViewer> CoreGlobalViewers { get; set; }
        
        // [v7.0] Wallet Architecture 분산화 엔티티
        DbSet<ViewerRelation> CoreViewerRelations { get; set; }
        DbSet<ViewerPoint> FuncViewerPoints { get; set; }
        DbSet<ViewerDonation> FuncViewerDonations { get; set; }
        DbSet<ViewerDonationHistory> FuncViewerDonationHistories { get; set; }
        DbSet<MooldangBot.Domain.Entities.Roulette> FuncRoulettes { get; set; }
        DbSet<RouletteItem> FuncRouletteItems { get; set; }
        DbSet<PeriodicMessage> SysPeriodicMessages { get; set; }
        DbSet<SonglistSession> FuncSonglistSessions { get; set; }
        DbSet<OverlayPreset> SysOverlayPresets { get; set; }
        DbSet<StreamerPreference> SysStreamerPreferences { get; set; }
        DbSet<BroadcastSession> SysBroadcastSessions { get; set; }
        DbSet<BroadcastHistoryLog> LogBroadcastHistory { get; set; }
        DbSet<UnifiedCommand> SysUnifiedCommands { get; set; }
        
        // IAMF Philosophy
        DbSet<IamfScenario> IamfScenarios { get; set; }
        DbSet<IamfGenosRegistry> IamfGenosRegistries { get; set; }
        DbSet<IamfParhosCycle> IamfParhosCycles { get; set; }
        DbSet<IamfVibrationLog> LogIamfVibrations { get; set; }
        DbSet<IamfStreamerSetting> IamfStreamerSettings { get; set; }
        DbSet<StreamerKnowledge> SysStreamerKnowledges { get; set; }

        DbSet<SharedComponent> SysSharedComponents { get; set; }
        DbSet<StreamerManager> CoreStreamerManagers { get; set; }
        DbSet<MooldangBot.Domain.Entities.SongBook> FuncSongBooks { get; set; }
        DbSet<Master_SongLibrary> FuncMasterSongLibraries { get; set; }
        DbSet<Streamer_SongLibrary> FuncStreamerSongLibraries { get; set; } // [v12.5] 스트리머 전용 라이브러리
        DbSet<Master_SongStaging> FuncMasterSongStagings { get; set; }
        DbSet<RouletteLog> FuncRouletteLogs { get; set; }
        DbSet<RouletteSpin> FuncRouletteSpins { get; set; } // [v1.9.9] 룰렛 영속성 레이어 추가
        DbSet<SoundAsset> FuncSoundAssets { get; set; }
        
        // [v11.1] 천상의 장부 (Celestial Ledger)
        DbSet<PointTransactionHistory> LogPointTransactions { get; set; }
        DbSet<PointDailySummary> LogPointDailySummaries { get; set; }
        DbSet<LogRouletteStats> LogRouletteStats { get; set; }
        DbSet<CommandExecutionLog> LogCommandExecutions { get; set; }
        DbSet<ChatInteractionLog> LogChatInteractions { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
