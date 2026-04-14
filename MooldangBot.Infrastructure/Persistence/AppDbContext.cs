using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Contracts.Roulette.Interfaces;
using MooldangBot.Contracts.SongBook.Interfaces;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Infrastructure.Persistence.Converters;
using MooldangBot.Domain.Common;
using Microsoft.AspNetCore.DataProtection;

namespace MooldangBot.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext, ISongBookDbContext, IRouletteDbContext, IPointDbContext, ICommandDbContext
{
    private readonly IUserSession _userSession;
    private readonly IDataProtector _protector;

    public AppDbContext(DbContextOptions<AppDbContext> options, 
                        IUserSession userSession,
                        IDataProtectionProvider provider) 
        : base(options)
    {
        _userSession = userSession;
        // [v4.0] 수호자의 의지: 용도(Purpose)를 명시하여 키 관리 격리
        _protector = provider.CreateProtector("MooldangBot.TokenEncryption.v1");
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // [v2.0] 모든 KstClock 타입 속성에 대해 전용 컨버터를 자동 적용합니다.
        configurationBuilder.Properties<KstClock>().HaveConversion<KstClockConverter>();
    }

    public DbSet<StreamerProfile> StreamerProfiles { get; set; }
    public DbSet<SongQueue> SongQueues { get; set; }
    public DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
    public DbSet<AvatarSetting> AvatarSettings { get; set; }
    public DbSet<ChzzkCategory> ChzzkCategories { get; set; }
    public DbSet<ChzzkCategoryAlias> ChzzkCategoryAliases { get; set; }
    public DbSet<GlobalViewer> GlobalViewers { get; set; }

    // [v7.0] Wallet Architecture 분산화 엔티티
    public DbSet<ViewerRelation> ViewerRelations { get; set; }
    public DbSet<ViewerPoint> ViewerPoints { get; set; }
    public DbSet<ViewerDonation> ViewerDonations { get; set; }
    public DbSet<ViewerDonationHistory> ViewerDonationHistories { get; set; }

    public DbSet<Roulette> Roulettes { get; set; }
    public DbSet<RouletteItem> RouletteItems { get; set; }
    public DbSet<PeriodicMessage> PeriodicMessages { get; set; }
    public DbSet<SonglistSession> SonglistSessions { get; set; }
    public DbSet<OverlayPreset> OverlayPresets { get; set; }
    public DbSet<StreamerPreference> StreamerPreferences { get; set; }

    // IAMF Philosophy Entities
    public DbSet<IamfScenario> IamfScenarios { get; set; }
    public DbSet<IamfGenosRegistry> IamfGenosRegistries { get; set; }
    public DbSet<IamfParhosCycle> IamfParhosCycles { get; set; }
    public DbSet<IamfVibrationLog> IamfVibrationLogs { get; set; }
    public DbSet<IamfStreamerSetting> IamfStreamerSettings { get; set; }
    public DbSet<StreamerKnowledge> StreamerKnowledges { get; set; }
    public DbSet<BroadcastSession> BroadcastSessions { get; set; }
    public DbSet<BroadcastHistoryLog> BroadcastHistoryLogs { get; set; }

    public DbSet<SharedComponent> SharedComponents { get; set; }
    public DbSet<StreamerManager> StreamerManagers { get; set; }
    public DbSet<SongBook> SongBooks { get; set; }
    public DbSet<RouletteLog> RouletteLogs { get; set; }
    public DbSet<RouletteSpin> RouletteSpins { get; set; } // [v1.9.9] 룰렛 영속성
    public DbSet<UnifiedCommand> UnifiedCommands { get; set; }
    public DbSet<Master_SongLibrary> MasterSongLibraries { get; set; }
    public DbSet<Streamer_SongLibrary> StreamerSongLibraries { get; set; } // [v12.5] 스트리머 라이브러리
    public DbSet<Master_SongStaging> MasterSongStagings { get; set; }
 
    // [v11.1] 천상의 장부 (Celestial Ledger)
    public DbSet<PointTransactionHistory> PointTransactionHistories { get; set; }
    public DbSet<PointDailySummary> PointDailySummaries { get; set; }
    public DbSet<RouletteStatsAggregated> RouletteStatsAggregated { get; set; }
    public DbSet<CommandExecutionLog> CommandExecutionLogs { get; set; }
    public DbSet<ChatInteractionLog> ChatInteractionLogs { get; set; }

    // [v13.1] 리포지토리 및 장부용 DbSet들 생략...
    // (DataProtectionKey DbSet 제거됨 - [v2.4.7] 파일 시스템 영속화로 전환)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // [v4.9.2] 전역 정렬 규칙 및 문자셋 통일: 모든 문자열 컬럼의 기본값을 utf8mb4_unicode_ci로 강제합니다.
        if (Database.IsMySql())
        {
            modelBuilder.HasCharSet("utf8mb4")
                        .UseCollation("utf8mb4_unicode_ci");
        }

        // [v4.0] 전역 암호화 컨버터 인스턴스 생성
        var converter = new EncryptedValueConverter(_protector);

        // [v6.1] 전역 쿼리 필터 자동화: ISoftDeletable을 구현하는 모든 엔티티에 대해 논리 삭제 필터 적용
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var body = System.Linq.Expressions.Expression.Equal(
                    System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
                    System.Linq.Expressions.Expression.Constant(false));
                
                var filter = System.Linq.Expressions.Expression.Lambda(body, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        modelBuilder.Entity<ChzzkCategory>()
            .HasMany(c => c.Aliases)
            .WithOne(a => a.Category)
            .HasForeignKey(a => a.CategoryId);

        modelBuilder.Entity<ChzzkCategoryAlias>()
            .HasIndex(a => a.Alias);

        // 🔍 대소문자 무관 검색을 위한 명시적 Collation 설정 (Osiris)
        var ciCollation = "utf8mb4_unicode_ci";

        modelBuilder.Entity<StreamerProfile>(entity => {
            if (Database.IsMySql())
            {
                entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
                entity.Property(e => e.Slug).UseCollation(ciCollation);
            }
        });



        modelBuilder.Entity<StreamerOmakaseItem>(entity => {
            entity.ToTable("song_list_omakases");
            entity.HasOne(o => o.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(o => o.StreamerProfileId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Roulette>(entity => {
            entity.ToTable("func_roulette_main");
            
            entity.HasOne(r => r.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(r => r.StreamerProfileId)
                  .IsRequired(false) // [오시리스의 자애]: 주인이 필터링되어도 경고 없이 처리
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.StreamerProfileId);
            entity.HasIndex(r => new { r.StreamerProfileId, r.Id }).IsDescending(false, true);
        });

        modelBuilder.Entity<AvatarSetting>(entity => {
            entity.ToTable("overlay_avatar_settings");
            entity.HasOne(a => a.StreamerProfile)
                  .WithOne()
                  .HasForeignKey<AvatarSetting>(a => a.StreamerProfileId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(a => a.StreamerProfileId).IsUnique();
        });

        modelBuilder.Entity<OverlayPreset>(entity => {
            entity.ToTable("overlay_presets");
            entity.HasOne(o => o.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(o => o.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(o => o.StreamerProfileId);
        });

        // [v11.1] 천상의 장부 매핑 설정
        modelBuilder.Entity<PointTransactionHistory>()
            .ToTable("log_point_transactions")
            .HasOne(p => p.StreamerProfile)
            .WithMany()
            .HasForeignKey(p => p.StreamerProfileId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PointDailySummary>()
            .ToTable("stats_point_daily");

        modelBuilder.Entity<RouletteStatsAggregated>()
            .ToTable("stats_roulette_audit");

        modelBuilder.Entity<CommandExecutionLog>()
            .ToTable("log_command_executions")
            .HasOne(c => c.StreamerProfile)
            .WithMany()
            .HasForeignKey(c => c.StreamerProfileId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatInteractionLog>(entity => {
            entity.ToTable("log_chat_interactions");
            entity.HasOne(c => c.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(c => c.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PeriodicMessage>(entity => {
            entity.ToTable("view_periodic_messages");
            entity.HasOne(m => m.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(m => m.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(m => m.StreamerProfileId);
        });

        modelBuilder.Entity<SharedComponent>(entity => {
            entity.ToTable("overlay_components");
            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => s.StreamerProfileId);
        });


        modelBuilder.Entity<RouletteLog>(entity => {
            entity.ToTable("func_roulette_logs");
            // [v6.2] ViewerNickname 필드 제거됨

            entity.HasOne(l => l.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(l => l.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.GlobalViewer)
                  .WithMany()
                  .HasForeignKey(l => l.GlobalViewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.RouletteItem)
                  .WithMany()
                  .HasForeignKey(l => l.RouletteItemId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.RouletteId);
            entity.HasIndex(e => new { e.StreamerProfileId, e.GlobalViewerId });
            
            // 🚀 [Phase 2] 커서 기반 페이지네이션 최적화
            entity.HasIndex(e => new { e.StreamerProfileId, e.Status, e.Id })
                  .IsDescending(false, false, true)
                  .HasDatabaseName("IX_RouletteLog_Status_Cursor");
        });

        modelBuilder.Entity<StreamerManager>(entity => {
            entity.ToTable("core_streamer_managers");
            
            entity.HasOne(m => m.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(m => m.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.GlobalViewer)
                  .WithMany()
                  .HasForeignKey(m => m.GlobalViewerId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ⭐ 검색 성능 최적화를 위한 인덱스 추가
        modelBuilder.Entity<StreamerProfile>(entity => {
            entity.HasIndex(p => p.ChzzkUid).IsUnique();
            entity.HasIndex(p => p.Slug).IsUnique();
        });
 
        // [v4.0] 수호자의 암호: 암호화 필드 설정 및 길이 확장
        modelBuilder.Entity<StreamerProfile>(entity => {
            entity.Property(e => e.ChzzkAccessToken).HasConversion(converter);
            entity.Property(e => e.ChzzkRefreshToken).HasConversion(converter);
        });
 
        // [v4.2] 글로벌 시청자 암호화 설정
        modelBuilder.Entity<GlobalViewer>(entity => {
            entity.ToTable("core_global_viewers");
            if (Database.IsMySql())
            {
                entity.Property(e => e.ViewerUid).HasColumnType("longtext").HasConversion(converter);
                entity.Property(e => e.Nickname).UseCollation(ciCollation); // [v6.2] 중앙 닉네임
            }
            else
            {
                entity.Property(e => e.ViewerUid).HasConversion(converter);
            }
            entity.Property(e => e.ViewerUidHash).HasMaxLength(64).IsRequired();
            
            // 🚀 [v6.2.2] 닉네임 기반 시청자 검색 성능 최적화 (오시리스의 눈)
            entity.HasIndex(e => e.Nickname).HasDatabaseName("IX_GlobalViewer_Nickname");
        });

        modelBuilder.Entity<ViewerRelation>(entity => {
            entity.ToTable("viewer_relations");
            entity.HasOne(v => v.StreamerProfile).WithMany().HasForeignKey(v => v.StreamerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.GlobalViewer).WithMany().HasForeignKey(v => v.GlobalViewerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ViewerPoint>(entity => {
            entity.ToTable("viewer_points");
            entity.HasOne(v => v.StreamerProfile).WithMany().HasForeignKey(v => v.StreamerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.GlobalViewer).WithMany().HasForeignKey(v => v.GlobalViewerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ViewerDonation>(entity => {
            entity.ToTable("viewer_donations");
            entity.HasOne(v => v.StreamerProfile).WithMany().HasForeignKey(v => v.StreamerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.GlobalViewer).WithMany().HasForeignKey(v => v.GlobalViewerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ViewerDonationHistory>(entity => {
            entity.ToTable("viewer_donations_history");
            entity.HasOne(v => v.StreamerProfile).WithMany().HasForeignKey(v => v.StreamerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.GlobalViewer).WithMany().HasForeignKey(v => v.GlobalViewerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RouletteSpin>(entity => {
            entity.ToTable("func_roulette_spins");
            
            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.GlobalViewer)
                  .WithMany()
                  .HasForeignKey(s => s.GlobalViewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.IsCompleted, e.ScheduledTime });
        });



        modelBuilder.Entity<SongQueue>(entity => {
            entity.ToTable("song_list_queues");

            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.GlobalViewer)
                  .WithMany()
                  .HasForeignKey(s => s.GlobalViewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // [v6.2.2] 노래책 연동 (선택 사항)
            entity.HasOne(s => s.SongBook)
                  .WithMany()
                  .HasForeignKey(s => s.SongBookId)
                  .OnDelete(DeleteBehavior.SetNull); // 노래책 항목이 삭제되어도 신청 기록은 유지
 
            entity.HasIndex(e => e.StreamerProfileId);
            entity.HasIndex(e => e.SongLibraryId); // [v13.1] Snowflake ID 검색 최적화

            // [v6.2.2] 상태값 Enum 변환
            entity.Property(e => e.Status).HasConversion<int>();
            
            // 🚀 [Phase 2] 커서 기반 페이지네이션 최적화
            entity.HasIndex(e => new { e.StreamerProfileId, e.Status, e.Id })
                  .IsDescending(false, false, true)
                  .HasDatabaseName("IX_SongQueue_Status_Cursor");
        });

        // [v4.5.1] 송북(노래책) 기본 매핑
        modelBuilder.Entity<SongBook>(entity => {
            entity.ToTable("song_book_main");
            entity.HasIndex(s => new { s.StreamerProfileId, s.Id });
        });

        // 🎵 [v12.0] 중앙 병기창 (Media Library) 매핑
        modelBuilder.Entity<Master_SongLibrary>(entity => {
            entity.ToTable("func_song_master_library");
            entity.HasIndex(e => e.SongLibraryId).IsUnique(); 
            entity.HasIndex(e => e.YoutubeUrl);
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.Alias);
            entity.HasIndex(e => e.TitleChosung);
            entity.HasIndex(e => e.ArtistChosung);
        });

        modelBuilder.Entity<Streamer_SongLibrary>(entity => {
            entity.ToTable("func_song_streamer_library");
            entity.HasIndex(e => e.SongLibraryId).IsUnique();
            entity.HasIndex(e => new { e.StreamerProfileId, e.SongLibraryId }).IsUnique();
        });

        modelBuilder.Entity<Master_SongStaging>(entity => {
            entity.ToTable("func_song_master_staging");
            entity.HasIndex(e => e.SongLibraryId).IsUnique(); 
            entity.HasIndex(e => e.CreatedAt); // [v13.1] 백그라운드 삭제 성능 향상
            entity.HasIndex(e => e.YoutubeUrl);
            entity.HasIndex(e => e.TitleChosung);
            entity.HasIndex(e => e.ArtistChosung);
            entity.Property(e => e.SourceType).HasConversion<int>();
        });

        modelBuilder.Entity<SonglistSession>(entity => {
            entity.ToTable("song_list_sessions");

            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.StreamerProfileId, s.IsActive });
        });

        // Legacy indices for RouletteLog removed (redefined above)

        // [파로스의 통합]: UnifiedCommand 설정 (v4.3 정형화 적용)
        modelBuilder.Entity<UnifiedCommand>(entity => {
            entity.ToTable("func_cmd_unified");
            entity.Property(e => e.Keyword).HasColumnName("keyword");
            if (Database.IsMySql())
            {
                entity.Property(e => e.Keyword).UseCollation(ciCollation);
            }
            entity.Property(e => e.CostType).HasConversion<string>();
            entity.Property(e => e.RequiredRole).HasConversion<string>();
            entity.Property(e => e.MatchType).HasConversion<string>();

            // 1. 스트리머 삭제 시 해당 채널의 명령어도 연쇄 삭제 (Cascade)
            entity.HasOne(c => c.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(c => c.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 2. 마스터 데이터 연동 제거 (Enum 기반으로 관리됨)
            entity.Property(e => e.FeatureType)
                  .HasConversion<string>()
                  .HasColumnName("feature_type");

            // [Index] 복합 인덱스: (StreamerProfileId, Keyword) 유니크 조합
            entity.HasIndex(e => new { e.StreamerProfileId, e.Keyword }).IsUnique();
            entity.HasIndex(e => new { e.StreamerProfileId, e.TargetId });

            // 🚀 [Phase 2] 커서 기반 페이지네이션 최적화
            entity.HasIndex(e => new { e.StreamerProfileId, e.Id })
                  .IsDescending(false, true)
                  .HasDatabaseName("IX_UnifiedCommand_CursorPaging");
        });


        modelBuilder.Entity<SongBook>().ToTable("song_book_main");
        modelBuilder.Entity<RouletteLog>().ToTable("func_roulette_logs");
        modelBuilder.Entity<StreamerProfile>().ToTable("core_streamer_profiles");
        modelBuilder.Entity<SongQueue>().ToTable("song_list_queues");
        modelBuilder.Entity<StreamerOmakaseItem>().ToTable("song_list_omakases");
        modelBuilder.Entity<AvatarSetting>().ToTable("overlay_avatar_settings");
        modelBuilder.Entity<ChzzkCategory>().ToTable("sys_chzzk_categories");
        modelBuilder.Entity<ChzzkCategoryAlias>().ToTable("sys_chzzk_category_aliases");
        modelBuilder.Entity<Roulette>().ToTable("func_roulette_main");
        modelBuilder.Entity<RouletteItem>(entity => {
            entity.ToTable("func_roulette_items");
            entity.HasOne(i => i.Roulette)
                  .WithMany(r => r.Items)
                  .HasForeignKey(i => i.RouletteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PeriodicMessage>().ToTable("view_periodic_messages");
        modelBuilder.Entity<SonglistSession>().ToTable("song_list_sessions");
        modelBuilder.Entity<OverlayPreset>().ToTable("overlay_presets");

        // IAMF Philosophy Mappings (Osiris Standard)
        modelBuilder.Entity<IamfScenario>().ToTable("iamf_scenarios");
        modelBuilder.Entity<IamfGenosRegistry>().ToTable("iamf_genos_registry");
        modelBuilder.Entity<IamfParhosCycle>().ToTable("iamf_parhos_cycles");

        modelBuilder.Entity<IamfVibrationLog>(entity => {
            entity.ToTable("iamf_vibration_logs");
            entity.HasOne(v => v.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(v => v.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
        });

        modelBuilder.Entity<IamfScenario>(entity => {
            entity.ToTable("iamf_scenarios");
            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
        });

        modelBuilder.Entity<IamfGenosRegistry>(entity => {
            entity.ToTable("iamf_genos_registry");
            entity.HasOne(g => g.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(g => g.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
        });

        modelBuilder.Entity<IamfParhosCycle>(entity => {
            entity.ToTable("iamf_parhos_cycles");
            
            // [v4.9] 복합 고유 인덱스 설정 (동시성 및 무결성 보장)
            entity.HasIndex(p => new { p.StreamerProfileId, p.CycleId }).IsUnique();

            entity.HasOne(p => p.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(p => p.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
        });

        modelBuilder.Entity<IamfStreamerSetting>(entity => {
            entity.ToTable("iamf_streamer_settings");
            entity.HasOne(s => s.StreamerProfile)
                  .WithOne()
                  .HasForeignKey<IamfStreamerSetting>(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BroadcastSession>(entity => {
            entity.ToTable("sys_broadcast_sessions");
            entity.HasOne(b => b.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(b => b.StreamerProfileId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BroadcastHistoryLog>(entity => {
            entity.ToTable("sys_broadcast_history_logs");
            entity.HasOne(h => h.BroadcastSession)
                  .WithMany()
                  .HasForeignKey(h => h.BroadcastSessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 🚀 [v6.2.2] 시계열 데이터 조회를 위한 복합 인덱스 (오시리스의 인덱싱)
            entity.HasIndex(h => new { h.BroadcastSessionId, h.LogDate });
        });

        modelBuilder.Entity<SharedComponent>().ToTable("overlay_components");

        modelBuilder.Entity<StreamerKnowledge>(entity => {
            entity.ToTable("streamer_knowledges");
            entity.HasOne(k => k.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(k => k.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // [v4.9.4] 함교 개인화 설정 (Permanent Preferences)
        modelBuilder.Entity<StreamerPreference>(entity => {
            entity.ToTable("sys_streamer_preferences");
            entity.HasOne(p => p.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(p => p.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // [오시리스의 인덱싱]: 사용자별 설정 키는 유니크해야 하며, 조회 성능을 위해 인덱싱함
            entity.HasIndex(p => new { p.StreamerProfileId, p.PreferenceKey }).IsUnique();
        });


        // [v6.1] 자식 엔티티의 필터는 암묵적 JOIN을 유발하므로 보수적으로 접근하나, 
        // ISoftDeletable 인터페이스 구현체에 한해 리플렉션으로 자동 주입됨 (상단 foreach 로직).
    }

    // [v6.1] 오시리스의 감시: 엔티티 저장 시 생성/수정 시간 및 논리 삭제 자동 처리
    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditAndSoftDelete()
    {
        var entries = ChangeTracker.Entries();
        var now = KstClock.Now;

        foreach (var entry in entries)
        {
            // 1. 감사 로그(Audit) 자동화
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        break;
                }
            }

            // 2. 논리적 삭제(Soft Delete) 인터셉터
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified; // 물리 삭제를 수정으로 변경
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
            }
        }
    }
}