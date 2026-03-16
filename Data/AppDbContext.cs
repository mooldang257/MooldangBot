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

        // ⭐ 새로 추가된 노래 대기열 테이블
        public DbSet<SongQueue> SongQueues { get; set; }
        public DbSet<StreamerCommand> StreamerCommands { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 리눅스/도커 환경 등에서의 대소문자 충돌 방지를 위해 소문자로 이름 고정
            modelBuilder.Entity<StreamerProfile>().ToTable("streamerprofiles");
            modelBuilder.Entity<SongQueue>().ToTable("songqueues");
            modelBuilder.Entity<SystemSetting>().ToTable("systemsettings");
    }

    }
}