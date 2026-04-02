using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Infrastructure.Persistence.Converters;
using MooldangBot.Domain.Common;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace MooldangBot.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext, IDataProtectionKeyContext
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
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<SongQueue> SongQueues { get; set; }
    public DbSet<StreamerOmakaseItem> StreamerOmakases { get; set; }
    public DbSet<AvatarSetting> AvatarSettings { get; set; }
    public DbSet<ChzzkCategory> ChzzkCategories { get; set; }
    public DbSet<ChzzkCategoryAlias> ChzzkCategoryAliases { get; set; }
    public DbSet<GlobalViewer> GlobalViewers { get; set; }
    public DbSet<ViewerProfile> ViewerProfiles { get; set; }
    public DbSet<Roulette> Roulettes { get; set; }
    public DbSet<RouletteItem> RouletteItems { get; set; }
    public DbSet<PeriodicMessage> PeriodicMessages { get; set; }
    public DbSet<SonglistSession> SonglistSessions { get; set; }
    public DbSet<OverlayPreset> OverlayPresets { get; set; }

    // IAMF Philosophy Entities
    public DbSet<IamfScenario> IamfScenarios { get; set; }
    public DbSet<IamfGenosRegistry> IamfGenosRegistries { get; set; }
    public DbSet<IamfParhosCycle> IamfParhosCycles { get; set; }
    public DbSet<IamfVibrationLog> IamfVibrationLogs { get; set; }
    public DbSet<IamfStreamerSetting> IamfStreamerSettings { get; set; }
    public DbSet<StreamerKnowledge> StreamerKnowledges { get; set; }
    public DbSet<BroadcastSession> BroadcastSessions { get; set; }

    public DbSet<SharedComponent> SharedComponents { get; set; }
    public DbSet<StreamerManager> StreamerManagers { get; set; }
    public DbSet<SongBook> SongBooks { get; set; }
    public DbSet<RouletteLog> RouletteLogs { get; set; }
    public DbSet<RouletteSpin> RouletteSpins { get; set; } // [v1.9.9] 룰렛 영속성
    public DbSet<UnifiedCommand> UnifiedCommands { get; set; }
    public DbSet<Master_CommandCategory> MasterCommandCategories { get; set; }
    public DbSet<Master_CommandFeature> MasterCommandFeatures { get; set; }
    public DbSet<Master_DynamicVariable> MasterDynamicVariables { get; set; }
 
    // DataProtectionKey 저장소 (Microsoft.AspNetCore.DataProtection.EntityFrameworkCore)
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // [v4.9.2] 전역 정렬 규칙 및 문자셋 통일: 모든 문자열 컬럼의 기본값을 utf8mb4_unicode_ci로 강제합니다.
        modelBuilder.HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_unicode_ci");

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
            entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
        });



        modelBuilder.Entity<StreamerOmakaseItem>(entity => {
            entity.ToTable("streamer_omakases");
            entity.HasOne(o => o.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(o => o.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Roulette>(entity => {
            entity.ToTable("roulettes");
            
            entity.HasOne(r => r.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(r => r.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.StreamerProfileId);
            entity.HasIndex(r => new { r.StreamerProfileId, r.Id }).IsDescending(false, true);
        });

        modelBuilder.Entity<AvatarSetting>(entity => {
            entity.ToTable("avatar_settings");
            entity.HasOne(a => a.StreamerProfile)
                  .WithOne()
                  .HasForeignKey<AvatarSetting>(a => a.StreamerProfileId)
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

        modelBuilder.Entity<PeriodicMessage>(entity => {
            entity.ToTable("periodic_messages");
            entity.HasOne(m => m.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(m => m.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(m => m.StreamerProfileId);
        });

        modelBuilder.Entity<SharedComponent>(entity => {
            entity.ToTable("shared_components");
            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => s.StreamerProfileId);
        });

        modelBuilder.Entity<ViewerProfile>(entity => {
            entity.Property(e => e.Nickname).UseCollation(ciCollation);
        });

        modelBuilder.Entity<RouletteLog>(entity => {
            entity.ToTable("roulette_logs");
            entity.Property(e => e.ViewerNickname).UseCollation(ciCollation);

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
            entity.HasIndex(e => new { e.StreamerProfileId, e.Status, e.Id })
                .IsDescending(false, false, true);
        });

        modelBuilder.Entity<StreamerManager>(entity => {
            entity.ToTable("streamer_managers");
            
            entity.HasOne(m => m.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(m => m.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.GlobalViewer)
                  .WithMany()
                  .HasForeignKey(m => m.GlobalViewerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ⭐ 검색 성능 최적화를 위한 인덱스 추가
        modelBuilder.Entity<StreamerProfile>()
            .HasIndex(p => p.ChzzkUid).IsUnique();
 
        // [v4.0] 수호자의 암호: 암호화 필드 설정 및 길이 확장
        modelBuilder.Entity<StreamerProfile>(entity => {
            entity.Property(e => e.ChzzkAccessToken).HasConversion(converter);
            entity.Property(e => e.ChzzkRefreshToken).HasConversion(converter);
            entity.Property(e => e.ApiClientId).HasColumnType("longtext").HasConversion(converter);
            entity.Property(e => e.ApiClientSecret).HasColumnType("longtext").HasConversion(converter);
            entity.Property(e => e.BotAccessToken).HasConversion(converter);
            entity.Property(e => e.BotRefreshToken).HasConversion(converter);
        });
 
        // [v4.2] 글로벌 시청자 암호화 설정
        modelBuilder.Entity<GlobalViewer>(entity => {
            entity.ToTable("global_viewers");
            entity.Property(e => e.ViewerUid).HasColumnType("longtext").HasConversion(converter);
            entity.Property(e => e.ViewerUidHash).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<ViewerProfile>(entity => {
            entity.ToTable("viewer_profiles");
            
            // 스트리머가 탈퇴/삭제되면 해당 방의 시청자 기록도 연쇄 삭제 (DB 용량 확보)
            entity.HasOne(v => v.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(v => v.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.GlobalViewer)
                  .WithMany()
                  .HasForeignKey(v => v.GlobalViewerId)
                  .OnDelete(DeleteBehavior.Restrict); // 글로벌 정보는 수동 관리 권장
        });

        modelBuilder.Entity<RouletteSpin>(entity => {
            entity.ToTable("roulette_spins");
            
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

        modelBuilder.Entity<SystemSetting>(entity => {
            entity.ToTable("system_settings");
            entity.Property(e => e.BotAccessToken).HasColumnType("longtext").HasConversion(converter);
            entity.Property(e => e.BotRefreshToken).HasColumnType("longtext").HasConversion(converter);
            entity.Property(e => e.KeyValue).HasColumnType("longtext").HasConversion(converter);
        });


        modelBuilder.Entity<SongQueue>(entity => {
            entity.ToTable("song_queues");

            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.GlobalViewer)
                  .WithMany()
                  .HasForeignKey(s => s.GlobalViewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.StreamerProfileId);
            entity.HasIndex(e => new { e.StreamerProfileId, e.Status, e.CreatedAt });
        });

        modelBuilder.Entity<SongBook>(entity => {
            entity.ToTable("song_books");

            entity.HasOne(s => s.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(s => s.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.StreamerProfileId, s.Id }).IsDescending(false, true);
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
            entity.ToTable("unified_commands");
            entity.Property(e => e.Keyword).HasColumnName("keyword").UseCollation(ciCollation);
            entity.Property(e => e.CostType).HasConversion<string>();
            entity.Property(e => e.RequiredRole).HasConversion<string>();

            // 1. 스트리머 삭제 시 해당 채널의 명령어도 연쇄 삭제 (Cascade)
            entity.HasOne(c => c.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(c => c.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 2. 마스터 데이터(기능) 삭제 시 명령어 데이터 보호 (Restrict)
            entity.HasOne(c => c.MasterFeature)
                  .WithMany()
                  .HasForeignKey(c => c.MasterCommandFeatureId)
                  .OnDelete(DeleteBehavior.Restrict);

            // [Index] 복합 인덱스: (StreamerProfileId, Keyword) 유니크 조합
            entity.HasIndex(e => new { e.StreamerProfileId, e.Keyword }).IsUnique();
            entity.HasIndex(e => new { e.StreamerProfileId, e.TargetId });
        });

        modelBuilder.Entity<Master_CommandCategory>(entity => {
            entity.ToTable("master_command_categories");
            entity.Property(e => e.Name).UseCollation(ciCollation);

            // [v1.7] 마스터 카테고리 재편
            entity.HasData(
                new Master_CommandCategory { Id = 1, Name = "General", DisplayName = "일반", SortOrder = 1 },
                new Master_CommandCategory { Id = 2, Name = "System", DisplayName = "시스템메세지", SortOrder = 2 },
                new Master_CommandCategory { Id = 3, Name = "Feature", DisplayName = "기능", SortOrder = 3 }
            );
        });

        modelBuilder.Entity<Master_CommandFeature>(entity => {
            entity.ToTable("master_command_features");
            entity.Property(e => e.TypeName).UseCollation(ciCollation);
            entity.Property(e => e.RequiredRole).HasConversion<string>();

            // [v1.7] 마스터 기능 재편 (9종)
            entity.HasData(
                new Master_CommandFeature { Id = 1, CategoryId = 1, TypeName = "Reply", DisplayName = "텍스트 답변", DefaultCost = 0, RequiredRole = CommandRole.Viewer },
                new Master_CommandFeature { Id = 2, CategoryId = 2, TypeName = "Notice", DisplayName = "공지", DefaultCost = 0, RequiredRole = CommandRole.Manager },
                new Master_CommandFeature { Id = 3, CategoryId = 2, TypeName = "Title", DisplayName = "방제", DefaultCost = 0, RequiredRole = CommandRole.Manager },
                new Master_CommandFeature { Id = 4, CategoryId = 2, TypeName = "Category", DisplayName = "카테고리", DefaultCost = 0, RequiredRole = CommandRole.Manager },
                new Master_CommandFeature { Id = 5, CategoryId = 2, TypeName = "SonglistToggle", DisplayName = "송리스트", DefaultCost = 0, RequiredRole = CommandRole.Manager },
                new Master_CommandFeature { Id = 6, CategoryId = 3, TypeName = "SongRequest", DisplayName = "노래신청", DefaultCost = 1000, RequiredRole = CommandRole.Viewer },
                new Master_CommandFeature { Id = 7, CategoryId = 3, TypeName = "Omakase", DisplayName = "오마카세", DefaultCost = 1000, RequiredRole = CommandRole.Viewer },
                new Master_CommandFeature { Id = 8, CategoryId = 3, TypeName = "Roulette", DisplayName = "룰렛", DefaultCost = 500, RequiredRole = CommandRole.Viewer },
                new Master_CommandFeature { Id = 9, CategoryId = 3, TypeName = "ChatPoint", DisplayName = "채팅포인트", DefaultCost = 0, RequiredRole = CommandRole.Viewer },
                new Master_CommandFeature { Id = 10, CategoryId = 2, TypeName = "SystemResponse", DisplayName = "시스템 응답", DefaultCost = 0, RequiredRole = CommandRole.Manager },
                new Master_CommandFeature { Id = 11, CategoryId = 3, TypeName = "AI", DisplayName = "AI 답변", DefaultCost = 1000, RequiredRole = CommandRole.Viewer }
            );
        });

        modelBuilder.Entity<Master_DynamicVariable>(entity => {
            entity.ToTable("master_dynamic_variables");
            entity.Property(e => e.Keyword).UseCollation(ciCollation);

            // [v1.8] 동적 변수 시딩 (Safe Query) & [v4.4.0] 내부 메서드 리졸버 매핑
            entity.HasData(
                new Master_DynamicVariable { 
                    Id = 1, 
                    Keyword = "{포인트}", 
                    Description = "보유 포인트", 
                    BadgeColor = "primary", 
                    QueryString = "SELECT CAST(vp.Points AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" 
                },
                new Master_DynamicVariable { 
                    Id = 2, 
                    Keyword = "{닉네임}", 
                    Description = "시청자 닉네임", 
                    BadgeColor = "success", 
                    QueryString = "SELECT vp.Nickname FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" 
                },
                new Master_DynamicVariable { 
                    Id = 3, 
                    Keyword = "{방제}", 
                    Description = "현재 방송 제목", 
                    BadgeColor = "secondary", 
                    QueryString = "METHOD:GetLiveTitle" 
                },
                new Master_DynamicVariable { 
                    Id = 4, 
                    Keyword = "{카테고리}", 
                    Description = "현재 방송 카테고리", 
                    BadgeColor = "info", 
                    QueryString = "METHOD:GetLiveCategory" 
                },
                new Master_DynamicVariable { 
                    Id = 5, 
                    Keyword = "{공지}", 
                    Description = "현재 방송 공지", 
                    BadgeColor = "warning", 
                    QueryString = "METHOD:GetLiveNotice" 
                },
                new Master_DynamicVariable { 
                    Id = 6, 
                    Keyword = "{연속출석일수}", 
                    Description = "연속 출석한 일수", 
                    BadgeColor = "success", 
                    QueryString = "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" 
                },
                new Master_DynamicVariable { 
                    Id = 7, 
                    Keyword = "{누적출석일수}", 
                    Description = "누적 출석한 횟수", 
                    BadgeColor = "info", 
                    QueryString = "SELECT CAST(vp.AttendanceCount AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" 
                },
                new Master_DynamicVariable { 
                    Id = 8, 
                    Keyword = "{마지막출석일}", 
                    Description = "최근 출석 날짜", 
                    BadgeColor = "secondary", 
                    QueryString = "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" 
                },
                new Master_DynamicVariable { 
                    Id = 10, 
                    Keyword = "{송리스트}", 
                    Description = "현재 송리스트 활성화 여부", 
                    BadgeColor = "warning", 
                    QueryString = "METHOD:GetSonglistStatus" 
                }
            );
        });

        modelBuilder.Entity<SongBook>().ToTable("song_books");
        modelBuilder.Entity<RouletteLog>().ToTable("roulette_logs");
        modelBuilder.Entity<StreamerProfile>().ToTable("streamer_profiles");
        modelBuilder.Entity<SongQueue>().ToTable("song_queues");
        modelBuilder.Entity<SystemSetting>().ToTable("system_settings");
        modelBuilder.Entity<StreamerOmakaseItem>().ToTable("streamer_omakases");
        modelBuilder.Entity<AvatarSetting>().ToTable("avatar_settings");
        modelBuilder.Entity<ChzzkCategory>().ToTable("chzzk_categories");
        modelBuilder.Entity<ChzzkCategoryAlias>().ToTable("chzzk_category_aliases");
        modelBuilder.Entity<ViewerProfile>().ToTable("viewer_profiles");
        modelBuilder.Entity<Roulette>().ToTable("roulettes");
        modelBuilder.Entity<RouletteItem>(entity => {
            entity.ToTable("roulette_items");
            entity.HasOne(i => i.Roulette)
                  .WithMany(r => r.Items)
                  .HasForeignKey(i => i.RouletteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PeriodicMessage>().ToTable("periodic_messages");
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
            entity.ToTable("broadcast_sessions");
            entity.HasOne(b => b.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(b => b.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SharedComponent>().ToTable("shared_components");

        modelBuilder.Entity<StreamerKnowledge>(entity => {
            entity.ToTable("streamer_knowledges");
            entity.HasOne(k => k.StreamerProfile)
                  .WithMany()
                  .HasForeignKey(k => k.StreamerProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
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