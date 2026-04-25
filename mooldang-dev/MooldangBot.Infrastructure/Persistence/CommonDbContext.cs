using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Infrastructure.Persistence.Converters;
using MooldangBot.Domain.Common;

namespace MooldangBot.Infrastructure.Persistence;

/// <summary>
/// [공용 라이브러리 컨텍스트]: 여러 서버/환경에서 공유하는 공용 자산 DB에 접근합니다.
/// </summary>
public class CommonDbContext : DbContext, ICommonDbContext
{
    public CommonDbContext(DbContextOptions<CommonDbContext> options) : base(options)
    {
    }

    public DbSet<CommonThumbnail> Thumbnails => Set<CommonThumbnail>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<KstClock>().HaveConversion<KstClockConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CommonThumbnail>(entity =>
        {
            entity.HasIndex(e => e.FileHash).IsUnique();
            entity.HasIndex(e => new { e.Artist, e.Title });
        });
    }
}
