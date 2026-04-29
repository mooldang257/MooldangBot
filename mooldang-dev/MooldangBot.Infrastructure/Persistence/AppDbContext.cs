using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Infrastructure.Persistence.Configurations;
using MooldangBot.Infrastructure.Persistence.Converters;
using MooldangBot.Infrastructure.Persistence.Extensions;
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
        // configurationBuilder.Properties<float[]>().HaveConversion<MariaDbVectorConverter>();
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

        // [v11.7-Fix] 벡터 필드는 Dapper에서 처리하므로 EF Core에서는 무시합니다.
        modelBuilder.Entity<Master_SongLibrary>().Ignore(x => x.TitleVector);
        modelBuilder.Entity<Streamer_SongLibrary>().Ignore(x => x.TitleVector);
        modelBuilder.Entity<SongBook>().Ignore(x => x.TitleVector);
        modelBuilder.Entity<Master_SongStaging>().Ignore(x => x.TitleVector);
    }
}