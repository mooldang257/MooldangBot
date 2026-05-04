using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MooldangBot.Infrastructure.Persistence.Configurations;

using MooldangBot.Infrastructure.Persistence.Extensions;
using MooldangBot.Infrastructure.Persistence.Converters;
using MooldangBot.Domain.Abstractions; 
using MooldangBot.Domain.Common; // KstClock 참조
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence;

// partial 키워드를 사용하여 DbSet 선언부를 분리했습니다.
public partial class AppDbContext : DbContext
{
    private readonly IDataProtector _protector;

    // 생성자 매개변수를 Pooling에 적합하게 조정
    public AppDbContext(
        DbContextOptions<AppDbContext> options, 
        IDataProtectionProvider provider) 
        : base(options)
    {
        _protector = provider.CreateProtector("MooldangBot.TokenEncryption.v1");
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<KstClock>().HaveConversion<KstClockConverter>();

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.IsMySql()) modelBuilder.HasCharSet("utf8mb4").UseCollation("utf8mb4_unicode_ci");

        modelBuilder.ApplySoftDeleteQueryFilter(); 

        var converter = new EncryptedValueConverter(_protector);
        modelBuilder.ApplyConfiguration(new StreamerProfileConfiguration(converter));
        modelBuilder.ApplyConfiguration(new GlobalViewerConfiguration(converter));
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // [v21.0-Fix] EF Core에서 벡터 필드를 완전히 격리합니다. (Dapper로만 처리)
        modelBuilder.Entity<FuncSongMasterLibrary>().Ignore(x => x.TitleVector);
        modelBuilder.Entity<FuncSongStreamerLibrary>().Ignore(x => x.TitleVector);
        modelBuilder.Entity<FuncSongBooks>().Ignore(x => x.TitleVector);
        modelBuilder.Entity<FuncSongMasterStaging>().Ignore(x => x.TitleVector);

        // [PascalCase Mapping]: 모든 엔티티를 클래스 이름과 동일한 테이블 이름으로 강제 매핑 (Table 접두사 충돌 방지)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder.Entity(entity.Name).ToTable(entity.ClrType.Name);
        }
    }
}