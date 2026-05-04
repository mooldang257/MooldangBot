using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

public class RouletteConfiguration : IEntityTypeConfiguration<FuncRouletteMain>
{
    public void Configure(EntityTypeBuilder<FuncRouletteMain> builder)
    {
        
        builder.HasOne(r => r.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(r => r.StreamerProfileId)
               .IsRequired(false) // [오시리스의 자애]: 주인이 필터링되어도 경고 없이 처리
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.StreamerProfileId);
        builder.HasIndex(r => new { r.StreamerProfileId, r.Id }).IsDescending(false, true);
    }
}

public class RouletteItemConfiguration : IEntityTypeConfiguration<FuncRouletteItems>
{
    public void Configure(EntityTypeBuilder<FuncRouletteItems> builder)
    {
        
        builder.HasOne(i => i.FuncRouletteMain)
               .WithMany(r => r.Items)
               .HasForeignKey(i => i.RouletteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RouletteLogConfiguration : IEntityTypeConfiguration<LogRouletteResults>
{
    public void Configure(EntityTypeBuilder<LogRouletteResults> builder)
    {
        // [v6.2] ViewerNickname 필드 제거됨

        builder.HasOne(l => l.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(l => l.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.CoreGlobalViewers)
               .WithMany()
               .HasForeignKey(l => l.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.FuncRouletteItems)
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
public class RouletteSpinConfiguration : IEntityTypeConfiguration<FuncRouletteSpins>
{
    public void Configure(EntityTypeBuilder<FuncRouletteSpins> builder)
    {
        
        builder.HasOne(s => s.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.CoreGlobalViewers)
               .WithMany()
               .HasForeignKey(s => s.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.IsCompleted, e.ScheduledTime });
    }
}
