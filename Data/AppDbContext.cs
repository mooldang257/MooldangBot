using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;

namespace MooldangAPI.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IUserSession _userSession;

        public AppDbContext(DbContextOptions<AppDbContext> options, IUserSession userSession) : base(options)
        {
            _userSession = userSession;
        }

        public DbSet<StreamerProfile> StreamerProfiles { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SongQueue> SongQueues { get; set; }
        public DbSet<StreamerCommand> StreamerCommands { get; set; }
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
        public DbSet<SharedComponent> SharedComponents { get; set; }
        public DbSet<StreamerManager> StreamerManagers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChzzkCategory>()
                .HasMany(c => c.Aliases)
                .WithOne(a => a.Category)
                .HasForeignKey(a => a.CategoryId);

            modelBuilder.Entity<ChzzkCategoryAlias>()
                .HasIndex(a => a.Alias);

            // ⭐ 검색 성능 최적화를 위한 인덱스 추가
            modelBuilder.Entity<StreamerProfile>()
                .HasIndex(p => p.ChzzkUid).IsUnique();
            
            modelBuilder.Entity<StreamerCommand>()
                .HasIndex(c => c.ChzzkUid);
            
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

            // 리눅스/도커 환경 등에서의 대소문자 충돌 방지를 위해 소문자로 이름 고정
            modelBuilder.Entity<StreamerProfile>().ToTable("streamerprofiles");
            modelBuilder.Entity<SongQueue>().ToTable("songqueues");
            modelBuilder.Entity<SystemSetting>().ToTable("systemsettings");
            modelBuilder.Entity<StreamerCommand>().ToTable("streamercommands");
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
            modelBuilder.Entity<SharedComponent>().ToTable("sharedcomponents");
            modelBuilder.Entity<StreamerManager>().ToTable("streamermanagers");

            // 🔐 멀티테넌트 데이터 격리를 위한 글로벌 쿼리 필터 자동 적용
            // 스트리머가 로그인된 경우, 본인의 ChzzkUid를 가진 데이터만 조회되도록 강제합니다.
            // 💡 [주의] 람다 식 내부에서 _userSession.ChzzkUid를 직접 참조해야 쿼리 실행 시점에 동적으로 값이 바뀝니다.
            
            modelBuilder.Entity<StreamerProfile>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<SongQueue>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<StreamerCommand>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<StreamerOmakaseItem>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<Roulette>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<PeriodicMessage>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<SonglistSession>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<OverlayPreset>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<SharedComponent>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
            modelBuilder.Entity<AvatarSetting>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);

            // 필드명이 다른 경우 예외 처리
            modelBuilder.Entity<ViewerProfile>().HasQueryFilter(e => !_userSession.IsAuthenticated || e.StreamerChzzkUid == _userSession.ChzzkUid);
        }

    }
}