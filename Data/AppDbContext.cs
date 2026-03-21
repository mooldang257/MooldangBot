using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;

namespace MooldangAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
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
                .HasIndex(r => r.ChzzkUid);

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
        }

    }
}