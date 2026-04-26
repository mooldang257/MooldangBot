using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

public class RouletteConfiguration : IEntityTypeConfiguration<Roulette>
{
    public void Configure(EntityTypeBuilder<Roulette> builder)
    {
        builder.ToTable("func_roulette_main");
        
        builder.HasOne(r => r.StreamerProfile)
               .WithMany()
               .HasForeignKey(r => r.StreamerProfileId)
               .IsRequired(false) // [오시리스의 자애]: 주인이 필터링되어도 경고 없이 처리
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.StreamerProfileId);
        builder.HasIndex(r => new { r.StreamerProfileId, r.Id }).IsDescending(false, true);
    }
}

public class RouletteItemConfiguration : IEntityTypeConfiguration<RouletteItem>
{
    public void Configure(EntityTypeBuilder<RouletteItem> builder)
    {
        builder.ToTable("func_roulette_items");
        
        builder.HasOne(i => i.Roulette)
               .WithMany(r => r.Items)
               .HasForeignKey(i => i.RouletteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RouletteLogConfiguration : IEntityTypeConfiguration<RouletteLog>
{
    public void Configure(EntityTypeBuilder<RouletteLog> builder)
    {
        builder.ToTable("log_roulette_results");
        // [v6.2] ViewerNickname 필드 제거됨

        builder.HasOne(l => l.StreamerProfile)
               .WithMany()
               .HasForeignKey(l => l.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.GlobalViewer)
               .WithMany()
               .HasForeignKey(l => l.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.RouletteItem)
               .WithMany()
               .HasForeignKey(l => l.RouletteItemId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.RouletteId);
        builder.HasIndex(e => new { e.StreamerProfileId, e.GlobalViewerId });
        
        // 🚀 [Phase 2] 커서 기반 페이지네이션 최적화
        builder.HasIndex(e => new { e.StreamerProfileId, e.Status, e.Id })
               .IsDescending(false, false, true)
               .HasDatabaseName("IX_RouletteLog_Status_Cursor");
    }
}

// [v1.9.9] 룰렛 영속성
public class RouletteSpinConfiguration : IEntityTypeConfiguration<RouletteSpin>
{
    public void Configure(EntityTypeBuilder<RouletteSpin> builder)
    {
        builder.ToTable("func_roulette_spins");
        
        builder.HasOne(s => s.StreamerProfile)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.GlobalViewer)
               .WithMany()
               .HasForeignKey(s => s.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.IsCompleted, e.ScheduledTime });
    }
}
