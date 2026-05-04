using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Domain.Abstractions
{
    public interface IAppDbContext
    {
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
        
        DbSet<CoreStreamerProfiles> TableCoreStreamerProfiles { get; set; }
        DbSet<FuncSongListQueues> TableFuncSongListQueues { get; set; }
        DbSet<FuncSongListOmakases> TableFuncSongListOmakases { get; set; }
        DbSet<SysAvatarSettings> TableSysAvatarSettings { get; set; }
        DbSet<SysChzzkCategories> TableSysChzzkCategories { get; set; }
        DbSet<SysChzzkCategoryAliases> TableSysChzzkCategoryAliases { get; set; }
        DbSet<CoreGlobalViewers> TableCoreGlobalViewers { get; set; }
        
        // [v7.0] Wallet Architecture 분산화 엔티티
        DbSet<CoreViewerRelations> TableCoreViewerRelations { get; set; }
        DbSet<FuncViewerPoints> TableFuncViewerPoints { get; set; }
        DbSet<FuncViewerDonations> TableFuncViewerDonations { get; set; }
        DbSet<FuncViewerDonationHistories> TableFuncViewerDonationHistories { get; set; }
        DbSet<FuncRouletteMain> TableFuncRouletteMain { get; set; }
        DbSet<FuncRouletteItems> TableFuncRouletteItems { get; set; }
        DbSet<SysPeriodicMessages> TableSysPeriodicMessages { get; set; }
        DbSet<FuncSongListSessions> TableFuncSongListSessions { get; set; }
        DbSet<SysOverlayPresets> TableSysOverlayPresets { get; set; }
        DbSet<SysStreamerPreferences> TableSysStreamerPreferences { get; set; }
        DbSet<SysBroadcastSessions> TableSysBroadcastSessions { get; set; }
        DbSet<LogBroadcastHistory> TableLogBroadcastHistory { get; set; }
        DbSet<FuncCmdUnified> TableFuncCmdUnified { get; set; }
        
        // IAMF Philosophy
        DbSet<IamfScenarios> TableIamfScenarios { get; set; }
        DbSet<IamfGenosRegistry> TableIamfGenosRegistry { get; set; }
        DbSet<IamfParhosCycles> TableIamfParhosCycles { get; set; }
        DbSet<LogIamfVibrations> TableLogIamfVibrations { get; set; }
        DbSet<IamfStreamerSettings> TableIamfStreamerSettings { get; set; }
        DbSet<SysStreamerKnowledges> TableSysStreamerKnowledges { get; set; }

        DbSet<SysSharedComponents> TableSysSharedComponents { get; set; }
        DbSet<CoreStreamerManagers> TableCoreStreamerManagers { get; set; }
        DbSet<FuncSongBooks> TableFuncSongBooks { get; set; }
        DbSet<GlobalMusicMetadata> TableGlobalMusicMetadata { get; set; }
        DbSet<FuncSongMasterLibrary> TableFuncSongMasterLibrary { get; set; }
        DbSet<FuncSongStreamerLibrary> TableFuncSongStreamerLibrary { get; set; } // [v12.5] 스트리머 전용 라이브러리
        DbSet<FuncSongMasterStaging> TableFuncSongMasterStaging { get; set; }
        DbSet<LogRouletteResults> TableLogRouletteResults { get; set; }
        DbSet<FuncRouletteSpins> TableFuncRouletteSpins { get; set; } // [v1.9.9] 룰렛 영속성 레이어 추가
        DbSet<FuncSoundAssets> TableFuncSoundAssets { get; set; }
        
        // [v11.1] 천상의 장부 (Celestial Ledger)
        DbSet<LogPointTransactions> TableLogPointTransactions { get; set; }
        DbSet<LogPointDailySummaries> TableLogPointDailySummaries { get; set; }
        DbSet<LogRouletteStats> TableLogRouletteStats { get; set; }
        DbSet<LogCommandExecutions> TableLogCommandExecutions { get; set; }
        DbSet<LogChatInteractions> TableLogChatInteractions { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
