using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        // [v19.0] 벡터 필드 고도화 (768d -> 3072d) 및 마이그레이션 활성화
        var vectorConverter = new MariaDbVectorConverter();
        var vectorComparer = new ValueComparer<float[]?>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? null : c.ToArray());
        
        modelBuilder.Entity<Master_SongLibrary>(entity => {
            entity.Property(x => x.TitleVector)
                .HasConversion(vectorConverter)
                .HasColumnType("VECTOR(3072)")
                .Metadata.SetValueComparer(vectorComparer);
            entity.Property(x => x.TitleVector).IsRequired();
        });

        modelBuilder.Entity<Streamer_SongLibrary>(entity => {
            entity.Property(x => x.TitleVector)
                .HasConversion(vectorConverter)
                .HasColumnType("VECTOR(3072)")
                .Metadata.SetValueComparer(vectorComparer);
        });

        modelBuilder.Entity<SongBook>(entity => {
            entity.Property(x => x.TitleVector)
                .HasConversion(vectorConverter)
                .HasColumnType("VECTOR(3072)")
                .Metadata.SetValueComparer(vectorComparer);
        });

        modelBuilder.Entity<Master_SongStaging>(entity => {
            entity.Property(x => x.TitleVector)
                .HasConversion(vectorConverter)
                .HasColumnType("VECTOR(3072)")
                .Metadata.SetValueComparer(vectorComparer);
        });
    }
}