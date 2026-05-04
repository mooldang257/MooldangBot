using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

// IAMF Philosophy Mappings (Osiris Standard)

public class IamfScenarioConfiguration : IEntityTypeConfiguration<IamfScenarios>
{
    public void Configure(EntityTypeBuilder<IamfScenarios> builder)
    {
        
        builder.HasOne(s => s.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfGenosRegistryConfiguration : IEntityTypeConfiguration<IamfGenosRegistry>
{
    public void Configure(EntityTypeBuilder<IamfGenosRegistry> builder)
    {
        
        builder.HasOne(g => g.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(g => g.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfParhosCycleConfiguration : IEntityTypeConfiguration<IamfParhosCycles>
{
    public void Configure(EntityTypeBuilder<IamfParhosCycles> builder)
    {
        
        // [v4.9] 복합 고유 인덱스 설정 (동시성 및 무결성 보장)
        builder.HasIndex(p => new { p.StreamerProfileId, p.CycleId }).IsUnique();

        builder.HasOne(p => p.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(p => p.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfVibrationLogConfiguration : IEntityTypeConfiguration<LogIamfVibrations>
{
    public void Configure(EntityTypeBuilder<LogIamfVibrations> builder)
    {
        
        builder.HasOne(v => v.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(v => v.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfStreamerSettingConfiguration : IEntityTypeConfiguration<IamfStreamerSettings>
{
    public void Configure(EntityTypeBuilder<IamfStreamerSettings> builder)
    {
        
        builder.HasOne(s => s.CoreStreamerProfiles)
               .WithOne()
               .HasForeignKey<IamfStreamerSettings>(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StreamerKnowledgeConfiguration : IEntityTypeConfiguration<SysStreamerKnowledges>
{
    public void Configure(EntityTypeBuilder<SysStreamerKnowledges> builder)
    {
        
        builder.HasOne(k => k.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(k => k.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BroadcastSessionConfiguration : IEntityTypeConfiguration<SysBroadcastSessions>
{
    public void Configure(EntityTypeBuilder<SysBroadcastSessions> builder)
    {
        
        builder.HasOne(b => b.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(b => b.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BroadcastHistoryLogConfiguration : IEntityTypeConfiguration<LogBroadcastHistory>
{
    public void Configure(EntityTypeBuilder<LogBroadcastHistory> builder)
    {
        
        builder.HasOne(h => h.SysBroadcastSessions)
               .WithMany()
               .HasForeignKey(h => h.BroadcastSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        // 🚀 [v6.2.2] 시계열 데이터 조회를 위한 복합 인덱스 (오시리스의 인덱싱)
        builder.HasIndex(h => new { h.BroadcastSessionId, h.LogDate });
    }
}