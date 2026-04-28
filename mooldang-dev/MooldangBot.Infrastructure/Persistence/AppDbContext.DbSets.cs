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
public partial class AppDbContext : IAppDbContext, ISongBookDbContext, IRouletteDbContext, IPointDbContext, ICommandDbContext
{
    // ────────── [Core & System] ──────────
    public DbSet<StreamerProfile> CoreStreamerProfiles { get; set; }
    public DbSet<GlobalViewer> CoreGlobalViewers { get; set; }
    public DbSet<ViewerRelation> CoreViewerRelations { get; set; }
    public DbSet<StreamerManager> CoreStreamerManagers { get; set; }
    public DbSet<ChzzkCategory> SysChzzkCategories { get; set; }
    public DbSet<ChzzkCategoryAlias> SysChzzkCategoryAliases { get; set; }
    public DbSet<StreamerPreference> SysStreamerPreferences { get; set; }
    public DbSet<PeriodicMessage> SysPeriodicMessages { get; set; }
    public DbSet<BroadcastSession> SysBroadcastSessions { get; set; }
    public DbSet<BroadcastHistoryLog> LogBroadcastHistory { get; set; }

    // ────────── [SongBook & Media] ──────────
    public DbSet<SongQueue> FuncSongQueues { get; set; }
    public DbSet<SongBook> FuncSongBooks { get; set; }

    public DbSet<SonglistSession> FuncSonglistSessions { get; set; }
    public DbSet<StreamerOmakaseItem> FuncStreamerOmakases { get; set; }
    public DbSet<Master_SongLibrary> FuncMasterSongLibraries { get; set; }
    public DbSet<Streamer_SongLibrary> FuncStreamerSongLibraries { get; set; }
    public DbSet<Master_SongStaging> FuncMasterSongStagings { get; set; }

    // ────────── [Roulette] ──────────
    public DbSet<Roulette> FuncRoulettes { get; set; }
    public DbSet<RouletteItem> FuncRouletteItems { get; set; }
    public DbSet<RouletteLog> FuncRouletteLogs { get; set; }
    public DbSet<RouletteSpin> FuncRouletteSpins { get; set; }
    public DbSet<SoundAsset> FuncSoundAssets { get; set; }

    // ────────── [Point & Wallet] ──────────
    public DbSet<ViewerPoint> FuncViewerPoints { get; set; }
    public DbSet<ViewerDonation> FuncViewerDonations { get; set; }
    public DbSet<ViewerDonationHistory> FuncViewerDonationHistories { get; set; }

    // ────────── [Commands & Saga] ──────────
    public DbSet<UnifiedCommand> SysUnifiedCommands { get; set; }
    public DbSet<CommandExecutionSagaState> CommandExecutionSagaStates { get; set; }

    // ────────── [Overlay] ──────────
    public DbSet<AvatarSetting> SysAvatarSettings { get; set; }
    public DbSet<OverlayPreset> SysOverlayPresets { get; set; }
    public DbSet<SharedComponent> SysSharedComponents { get; set; }

    // ────────── [IAMF Philosophy] ──────────
    public DbSet<IamfScenario> IamfScenarios { get; set; }
    public DbSet<IamfGenosRegistry> IamfGenosRegistries { get; set; }
    public DbSet<IamfParhosCycle> IamfParhosCycles { get; set; }
    public DbSet<IamfVibrationLog> LogIamfVibrations { get; set; }
    public DbSet<IamfStreamerSetting> IamfStreamerSettings { get; set; }
    public DbSet<StreamerKnowledge> SysStreamerKnowledges { get; set; }

    // ────────── [Ledger & Stats (천상의 장부)] ──────────
    public DbSet<PointTransactionHistory> LogPointTransactions { get; set; }
    public DbSet<PointDailySummary> LogPointDailySummaries { get; set; }
    public DbSet<LogRouletteStats> LogRouletteStats { get; set; }
    public DbSet<CommandExecutionLog> LogCommandExecutions { get; set; }
    public DbSet<ChatInteractionLog> LogChatInteractions { get; set; }
}