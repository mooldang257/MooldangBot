using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Roulette.Abstractions;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Infrastructure.Sagas;

namespace MooldangBot.Infrastructure.Persistence;

// partial 클래스를 사용하여 기존 AppDbContext의 덩치를 줄입니다.
public partial class AppDbContext : IAppDbContext, ISongBookDbContext, IRouletteDbContext, IPointDbContext, ICommandDbContext, ICommonDbContext
{
    // ────────── [Core & System] ──────────
    public DbSet<CoreStreamerProfiles> TableCoreStreamerProfiles { get; set; }
    public DbSet<CoreGlobalViewers> TableCoreGlobalViewers { get; set; }
    public DbSet<CoreViewerRelations> TableCoreViewerRelations { get; set; }
    public DbSet<CoreStreamerManagers> TableCoreStreamerManagers { get; set; }
    public DbSet<SysChzzkCategories> TableSysChzzkCategories { get; set; }
    public DbSet<SysChzzkCategoryAliases> TableSysChzzkCategoryAliases { get; set; }
    public DbSet<SysStreamerPreferences> TableSysStreamerPreferences { get; set; }
    public DbSet<SysPeriodicMessages> TableSysPeriodicMessages { get; set; }
    public DbSet<SysBroadcastSessions> TableSysBroadcastSessions { get; set; }
    public DbSet<LogBroadcastHistory> TableLogBroadcastHistory { get; set; }

    // ────────── [FuncSongBooks & Media] ──────────
    public DbSet<FuncSongListQueues> TableFuncSongListQueues { get; set; }
    public DbSet<FuncSongBooks> TableFuncSongBooks { get; set; }

    public DbSet<FuncSongListSessions> TableFuncSongListSessions { get; set; }
    public DbSet<FuncSongListOmakases> TableFuncSongListOmakases { get; set; }
    public DbSet<FuncSongMasterLibrary> TableFuncSongMasterLibrary { get; set; }
    public DbSet<FuncSongStreamerLibrary> TableFuncSongStreamerLibrary { get; set; }
    public DbSet<FuncSongMasterStaging> TableFuncSongMasterStaging { get; set; }
    public DbSet<GlobalMusicMetadata> TableGlobalMusicMetadata { get; set; }

    // ────────── [FuncRouletteMain] ──────────
    public DbSet<FuncRouletteMain> TableFuncRouletteMain { get; set; }
    public DbSet<FuncRouletteItems> TableFuncRouletteItems { get; set; }
    public DbSet<LogRouletteResults> TableLogRouletteResults { get; set; }
    public DbSet<FuncRouletteSpins> TableFuncRouletteSpins { get; set; }
    public DbSet<FuncSoundAssets> TableFuncSoundAssets { get; set; }

    // ────────── [Point & Wallet] ──────────
    public DbSet<FuncViewerPoints> TableFuncViewerPoints { get; set; }
    public DbSet<FuncViewerDonations> TableFuncViewerDonations { get; set; }
    public DbSet<FuncViewerDonationHistories> TableFuncViewerDonationHistories { get; set; }

    // ────────── [Commands & Saga] ──────────
    public DbSet<FuncCmdUnified> TableFuncCmdUnified { get; set; }
    public DbSet<SysSagaCommandExecutions> TableSysSagaCommandExecutions { get; set; }

    // ────────── [Overlay] ──────────
    public DbSet<SysAvatarSettings> TableSysAvatarSettings { get; set; }
    public DbSet<SysOverlayPresets> TableSysOverlayPresets { get; set; }
    public DbSet<SysSharedComponents> TableSysSharedComponents { get; set; }

    // ────────── [Common] ──────────
    public DbSet<CommonThumbnail> TableCommonThumbnail { get; set; }

    // ────────── [IAMF Philosophy] ──────────
    public DbSet<IamfScenarios> TableIamfScenarios { get; set; }
    public DbSet<IamfGenosRegistry> TableIamfGenosRegistry { get; set; }
    public DbSet<IamfParhosCycles> TableIamfParhosCycles { get; set; }
    public DbSet<LogIamfVibrations> TableLogIamfVibrations { get; set; }
    public DbSet<IamfStreamerSettings> TableIamfStreamerSettings { get; set; }
    public DbSet<SysStreamerKnowledges> TableSysStreamerKnowledges { get; set; }

    // ────────── [Ledger & Stats (천상의 장부)] ──────────
    public DbSet<LogPointTransactions> TableLogPointTransactions { get; set; }
    public DbSet<LogPointDailySummaries> TableLogPointDailySummaries { get; set; }
    public DbSet<LogRouletteStats> TableLogRouletteStats { get; set; }
    public DbSet<LogCommandExecutions> TableLogCommandExecutions { get; set; }
    public DbSet<LogChatInteractions> TableLogChatInteractions { get; set; }
}