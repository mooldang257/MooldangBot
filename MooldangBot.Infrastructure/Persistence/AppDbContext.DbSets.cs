using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Modules.Roulette.Abstractions;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Infrastructure.Sagas;

namespace MooldangBot.Infrastructure.Persistence;

// partial 클래스를 사용하여 기존 AppDbContext의 덩치를 줄입니다.
public partial class AppDbContext : IAppDbContext, ISongBookDbContext, IRouletteDbContext, IPointDbContext, ICommandDbContext
{
    // ────────── [Core & System] ──────────
    public DbSet<StreamerProfile> StreamerProfiles { get; set; }
    public DbSet<GlobalViewer> GlobalViewers { get; set; }
    public DbSet<ViewerRelation> ViewerRelations { get; set; }
    public DbSet<StreamerManager> StreamerManagers { get; set; }
    public DbSet<ChzzkCategory> ChzzkCategories { get; set; }
    public DbSet<ChzzkCategoryAlias> ChzzkCategoryAliases { get; set; }
    public DbSet<StreamerPreference> StreamerPreferences { get; set; }
    public DbSet<PeriodicMessage> PeriodicMessages { get; set; }
    public DbSet<BroadcastSession> BroadcastSessions { get; set; }
    public DbSet<BroadcastHistoryLog> BroadcastHistoryLogs { get; set; }

    // ────────── [SongBook & Media] ──────────
    public DbSet<SongQueue> SongQueues { get; set; }
    public DbSet<SongBook> SongBooks { get; set; }
    public DbSet<SonglistSession> SonglistSessions { get; set; }
    public DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
    public DbSet<Master_SongLibrary> MasterSongLibraries { get; set; }
    public DbSet<Streamer_SongLibrary> StreamerSongLibraries { get; set; }
    public DbSet<Master_SongStaging> MasterSongStagings { get; set; }

    // ────────── [Roulette] ──────────
    public DbSet<Roulette> Roulettes { get; set; }
    public DbSet<RouletteItem> RouletteItems { get; set; }
    public DbSet<RouletteLog> RouletteLogs { get; set; }
    public DbSet<RouletteSpin> RouletteSpins { get; set; }
    public DbSet<SoundAsset> SoundAssets { get; set; }

    // ────────── [Point & Wallet] ──────────
    public DbSet<ViewerPoint> ViewerPoints { get; set; }
    public DbSet<ViewerDonation> ViewerDonations { get; set; }
    public DbSet<ViewerDonationHistory> ViewerDonationHistories { get; set; }

    // ────────── [Commands & Saga] ──────────
    public DbSet<UnifiedCommand> UnifiedCommands { get; set; }
    public DbSet<CommandExecutionSagaState> CommandExecutionSagaStates { get; set; }

    // ────────── [Overlay] ──────────
    public DbSet<AvatarSetting> AvatarSettings { get; set; }
    public DbSet<OverlayPreset> OverlayPresets { get; set; }
    public DbSet<SharedComponent> SharedComponents { get; set; }

    // ────────── [IAMF Philosophy] ──────────
    public DbSet<IamfScenario> IamfScenarios { get; set; }
    public DbSet<IamfGenosRegistry> IamfGenosRegistries { get; set; }
    public DbSet<IamfParhosCycle> IamfParhosCycles { get; set; }
    public DbSet<IamfVibrationLog> IamfVibrationLogs { get; set; }
    public DbSet<IamfStreamerSetting> IamfStreamerSettings { get; set; }
    public DbSet<StreamerKnowledge> StreamerKnowledges { get; set; }

    // ────────── [Ledger & Stats (천상의 장부)] ──────────
    public DbSet<PointTransactionHistory> PointTransactionHistories { get; set; }
    public DbSet<PointDailySummary> PointDailySummaries { get; set; }
    public DbSet<RouletteStatsAggregated> RouletteStatsAggregated { get; set; }
    public DbSet<CommandExecutionLog> CommandExecutionLogs { get; set; }
    public DbSet<ChatInteractionLog> ChatInteractionLogs { get; set; }
}