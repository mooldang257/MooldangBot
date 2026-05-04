using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Foundation.Persistence;

/// <summary>
/// [파운데이션]: 시스템의 가장 기초적인 데이터(스트리머 정보, 시청자 기본 정보 등)를 담당하는 DbContext입니다.
/// 비즈니스 모듈(노래책, 룰렛 등)에 의존하지 않습니다.
/// </summary>
public class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
    {
    }

    // ────────── [Core & System Entities] ──────────
    public DbSet<CoreStreamerProfiles> TableCoreStreamerProfiles { get; set; }
    public DbSet<CoreGlobalViewers> TableCoreGlobalViewers { get; set; }
    public DbSet<CoreViewerRelations> TableCoreViewerRelations { get; set; }
    public DbSet<CoreStreamerManagers> TableCoreStreamerManagers { get; set; }
    public DbSet<SysChzzkCategories> TableSysChzzkCategories { get; set; }
    public DbSet<SysChzzkCategoryAliases> TableSysChzzkCategoryAliases { get; set; }
    public DbSet<SysStreamerPreferences> TableSysStreamerPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.IsMySql())
        {
            modelBuilder.HasCharSet("utf8mb4").UseCollation("utf8mb4_unicode_ci");
        }

        // [PascalCase Mapping]: 모든 엔티티를 클래스 이름과 동일한 테이블 이름으로 매핑
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder.Entity(entity.Name).ToTable(entity.ClrType.Name);
        }
        
        // 추가적인 Core 설정은 여기에 작성하거나 Configuration 클래스를 사용합니다.
    }
}
