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

        // [v4.0] 전역 암호화 컨버터 인스턴스 생성
        var converter = new EncryptedValueConverter(_protector);

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

        modelBuilder.Entity<SongQueue>(entity => {
            entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
        });


        modelBuilder.Entity<StreamerOmakaseItem>(entity => {
            entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
        });

        modelBuilder.Entity<Roulette>(entity => {
            entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
        });

        modelBuilder.Entity<PeriodicMessage>(entity => {
            entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
        });

        modelBuilder.Entity<ViewerProfile>(entity => {
            entity.Property(e => e.StreamerChzzkUid).UseCollation(ciCollation);
        });

        modelBuilder.Entity<RouletteLog>(entity => {
            entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
            entity.Property(e => e.ViewerNickname).UseCollation(ciCollation);
        });

        modelBuilder.Entity<StreamerManager>(entity => {
            entity.Property(e => e.StreamerChzzkUid).UseCollation(ciCollation);
            entity.Property(e => e.ManagerChzzkUid).UseCollation(ciCollation);
        });

        // ⭐ 검색 성능 최적화를 위한 인덱스 추가
        modelBuilder.Entity<StreamerProfile>()
            .HasIndex(p => p.ChzzkUid).IsUnique();
 
        // [v4.0] 수호자의 암호: 암호화 필드 설정 및 길이 확장
        modelBuilder.Entity<StreamerProfile>(entity => {
            entity.Property(e => e.ChzzkAccessToken).HasConversion(converter);
            entity.Property(e => e.ChzzkRefreshToken).HasConversion(converter);
            entity.Property(e => e.ApiClientId).HasColumnType("longtext");
            entity.Property(e => e.ApiClientSecret).HasColumnType("longtext").HasConversion(converter);
            entity.Property(e => e.BotAccessToken).HasConversion(converter);
            entity.Property(e => e.BotRefreshToken).HasConversion(converter);
        });
 
        modelBuilder.Entity<ViewerProfile>(entity => {
            // [Search Hash 전략]: 원본 Uid는 암호화, 검색은 Hash 필드 이용
            entity.Property(e => e.ViewerUid).HasColumnType("longtext").HasConversion(converter);
            entity.Property(e => e.ViewerUidHash).HasMaxLength(64).IsRequired();
            
            entity.HasIndex(e => new { e.StreamerChzzkUid, e.ViewerUidHash }).IsUnique();
        });

        modelBuilder.Entity<RouletteSpin>(entity => {
            entity.Property(e => e.ViewerUid).HasColumnType("longtext").HasConversion(converter);
        });

        modelBuilder.Entity<SystemSetting>(entity => {
            entity.Property(e => e.BotAccessToken).HasConversion(converter);
            entity.Property(e => e.BotRefreshToken).HasConversion(converter);
            entity.Property(e => e.KeyValue).HasConversion(converter);
        });


        modelBuilder.Entity<SongQueue>()
            .HasIndex(s => s.ChzzkUid);

        modelBuilder.Entity<SongQueue>()
            .HasIndex(s => s.ChzzkUid);

        modelBuilder.Entity<Roulette>()
            .HasIndex(r => new { r.ChzzkUid, r.Id }).IsDescending(false, true);

        modelBuilder.Entity<PeriodicMessage>()
            .HasIndex(p => p.ChzzkUid);

        modelBuilder.Entity<SonglistSession>()
            .HasIndex(s => new { s.ChzzkUid, s.IsActive });

        modelBuilder.Entity<OverlayPreset>()
            .HasIndex(p => p.ChzzkUid);

        modelBuilder.Entity<SharedComponent>()
            .HasIndex(c => c.ChzzkUid);

        modelBuilder.Entity<StreamerManager>()
            .HasIndex(m => m.ManagerChzzkUid);

        modelBuilder.Entity<SongBook>()
            .HasIndex(s => new { s.ChzzkUid, s.Id }).IsDescending(false, true);

        modelBuilder.Entity<RouletteLog>()
            .HasIndex(l => l.RouletteId);

        modelBuilder.Entity<RouletteLog>()
            .HasIndex(l => new { l.ChzzkUid, l.Status, l.Id })
            .IsDescending(false, false, true);

        // [파로스의 통합]: UnifiedCommand 설정 (Osiris Regulation)
        modelBuilder.Entity<UnifiedCommand>(entity => {
            entity.ToTable("unifiedcommands");
            entity.Property(e => e.ChzzkUid).HasColumnName("chzzkuid").UseCollation(ciCollation);
            entity.Property(e => e.Keyword).HasColumnName("keyword").UseCollation(ciCollation);
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.CostType).HasConversion<string>();
            entity.Property(e => e.RequiredRole).HasConversion<string>();
        });
        modelBuilder.Entity<UnifiedCommand>().HasIndex(c => new { c.ChzzkUid, c.Keyword }).IsUnique();
        modelBuilder.Entity<UnifiedCommand>().HasIndex(c => new { c.ChzzkUid, c.TargetId });

        modelBuilder.Entity<Master_CommandCategory>(entity => {
            entity.ToTable("master_commandcategories");
            entity.Property(e => e.Name).UseCollation(ciCollation);

            // [v1.7] 마스터 카테고리 재편
            entity.HasData(
                new Master_CommandCategory { Id = 1, Name = "General", DisplayName = "일반", SortOrder = 1 },
                new Master_CommandCategory { Id = 2, Name = "System", DisplayName = "시스템메세지", SortOrder = 2 },
                new Master_CommandCategory { Id = 3, Name = "Feature", DisplayName = "기능", SortOrder = 3 }
            );
        });

        modelBuilder.Entity<Master_CommandFeature>(entity => {
            entity.ToTable("master_commandfeatures");
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
            entity.ToTable("master_dynamicvariables");
            entity.Property(e => e.Keyword).UseCollation(ciCollation);

            // [v1.8] 동적 변수 시딩 (Safe Query) & [v4.4.0] 내부 메서드 리졸버 매핑
            entity.HasData(
                new Master_DynamicVariable { 
                    Id = 1, 
                    Keyword = "{포인트}", 
                    Description = "보유 포인트", 
                    BadgeColor = "primary", 
                    QueryString = "SELECT CAST(Point AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" 
                },
                new Master_DynamicVariable { 
                    Id = 2, 
                    Keyword = "{닉네임}", 
                    Description = "시청자 닉네임", 
                    BadgeColor = "success", 
                    QueryString = "SELECT ViewerName FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" 
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
                    QueryString = "SELECT CAST(ConsecutiveAttendanceCount AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" 
                },
                new Master_DynamicVariable { 
                    Id = 7, 
                    Keyword = "{누적출석일수}", 
                    Description = "누적 출석한 횟수", 
                    BadgeColor = "info", 
                    QueryString = "SELECT CAST(AttendanceCount AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" 
                },
                new Master_DynamicVariable { 
                    Id = 8, 
                    Keyword = "{마지막출석일}", 
                    Description = "최근 출석 날짜", 
                    BadgeColor = "secondary", 
                    QueryString = "SELECT DATE_FORMAT(LastAttendanceAt, '%Y-%m-%d %H:%i') FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" 
                },
                new Master_DynamicVariable { 
                    Id = 9, 
                    Keyword = "{송리스트}", 
                    Description = "현재 송리스트 활성화 여부", 
                    BadgeColor = "warning", 
                    QueryString = "METHOD:GetSonglistStatus" 
                }
            );
        });

        modelBuilder.Entity<SongBook>().ToTable("songbooks");
        modelBuilder.Entity<RouletteLog>().ToTable("roulettelogs");
        modelBuilder.Entity<StreamerProfile>().ToTable("streamerprofiles");
        modelBuilder.Entity<SongQueue>().ToTable("songqueues");
        modelBuilder.Entity<SystemSetting>().ToTable("systemsettings");
        modelBuilder.Entity<StreamerOmakaseItem>().ToTable("streameromakases");
        modelBuilder.Entity<AvatarSetting>().ToTable("avatarsettings");
        modelBuilder.Entity<ChzzkCategory>().ToTable("chzzkcategories");
        modelBuilder.Entity<ChzzkCategoryAlias>().ToTable("chzzkcategoryaliases");
        modelBuilder.Entity<ViewerProfile>().ToTable("viewerprofiles");
        modelBuilder.Entity<Roulette>().ToTable("roulettes");
        modelBuilder.Entity<RouletteItem>().ToTable("rouletteitems");
        modelBuilder.Entity<PeriodicMessage>().ToTable("periodicmessages");
        modelBuilder.Entity<SonglistSession>().ToTable("songlistsessions");
        modelBuilder.Entity<OverlayPreset>().ToTable("overlaypresets");

        // IAMF Philosophy Mappings (Osiris Standard)
        modelBuilder.Entity<IamfScenario>().ToTable("iamf_scenarios");
        modelBuilder.Entity<IamfGenosRegistry>().ToTable("iamf_genos_registry");
        modelBuilder.Entity<IamfParhosCycle>().ToTable("iamf_parhos_cycles");
        modelBuilder.Entity<IamfVibrationLog>().ToTable("iamf_vibration_logs");
        modelBuilder.Entity<IamfStreamerSetting>().ToTable("iamf_streamer_settings");
        modelBuilder.Entity<BroadcastSession>().ToTable("broadcastsessions");

        modelBuilder.Entity<SharedComponent>().ToTable("sharedcomponents");
        modelBuilder.Entity<StreamerManager>().ToTable("streamermanagers");
        modelBuilder.Entity<SongBook>().ToTable("songbooks");
        modelBuilder.Entity<RouletteLog>().ToTable("roulettelogs");
        modelBuilder.Entity<RouletteSpin>(entity => {
            entity.ToTable("roulettespins");
            entity.Property(e => e.ChzzkUid).UseCollation(ciCollation);
            entity.HasIndex(e => new { e.IsCompleted, e.ScheduledTime });
        });

        // 🔐 멀티테넌트 데이터 격리를 위한 글로벌 쿼리 필터 자동 적용
        modelBuilder.Entity<StreamerProfile>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<SongQueue>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<StreamerOmakaseItem>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<Roulette>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<PeriodicMessage>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<SonglistSession>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<OverlayPreset>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<SharedComponent>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<AvatarSetting>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<SongBook>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<RouletteLog>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<RouletteSpin>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<BroadcastSession>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
        modelBuilder.Entity<UnifiedCommand>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);

        // 필드명이 다른 경우 예외 처리
        modelBuilder.Entity<ViewerProfile>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.StreamerChzzkUid == _userSession.ChzzkUid);
    }
}