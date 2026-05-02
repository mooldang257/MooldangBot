using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

// IAMF Philosophy Mappings (Osiris Standard)

public class IamfScenarioConfiguration : IEntityTypeConfiguration<IamfScenario>
{
    public void Configure(EntityTypeBuilder<IamfScenario> builder)
    {
        builder.ToTable("IamfScenarios");
        
        builder.HasOne(s => s.StreamerProfile)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfGenosRegistryConfiguration : IEntityTypeConfiguration<IamfGenosRegistry>
{
    public void Configure(EntityTypeBuilder<IamfGenosRegistry> builder)
    {
        builder.ToTable("IamfGenosRegistry");
        
        builder.HasOne(g => g.StreamerProfile)
               .WithMany()
               .HasForeignKey(g => g.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfParhosCycleConfiguration : IEntityTypeConfiguration<IamfParhosCycle>
{
    public void Configure(EntityTypeBuilder<IamfParhosCycle> builder)
    {
        builder.ToTable("IamfParhosCycles");
        
        // [v4.9] 복합 고유 인덱스 설정 (동시성 및 무결성 보장)
        builder.HasIndex(p => new { p.StreamerProfileId, p.CycleId }).IsUnique();

        builder.HasOne(p => p.StreamerProfile)
               .WithMany()
               .HasForeignKey(p => p.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfVibrationLogConfiguration : IEntityTypeConfiguration<IamfVibrationLog>
{
    public void Configure(EntityTypeBuilder<IamfVibrationLog> builder)
    {
        builder.ToTable("LogIamfVibrations");
        
        builder.HasOne(v => v.StreamerProfile)
               .WithMany()
               .HasForeignKey(v => v.StreamerProfileId)
               .OnDelete(DeleteBehavior.Restrict); // [v4.9] 존재의 보존
    }
}

public class IamfStreamerSettingConfiguration : IEntityTypeConfiguration<IamfStreamerSetting>
{
    public void Configure(EntityTypeBuilder<IamfStreamerSetting> builder)
    {
        builder.ToTable("IamfStreamerSettings");
        
        builder.HasOne(s => s.StreamerProfile)
               .WithOne()
               .HasForeignKey<IamfStreamerSetting>(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StreamerKnowledgeConfiguration : IEntityTypeConfiguration<StreamerKnowledge>
{
    public void Configure(EntityTypeBuilder<StreamerKnowledge> builder)
    {
        builder.ToTable("SysStreamerKnowledges");
        
        builder.HasOne(k => k.StreamerProfile)
               .WithMany()
               .HasForeignKey(k => k.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BroadcastSessionConfiguration : IEntityTypeConfiguration<BroadcastSession>
{
    public void Configure(EntityTypeBuilder<BroadcastSession> builder)
    {
        builder.ToTable("SysBroadcastSessions");
        
        builder.HasOne(b => b.StreamerProfile)
               .WithMany()
               .HasForeignKey(b => b.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BroadcastHistoryLogConfiguration : IEntityTypeConfiguration<BroadcastHistoryLog>
{
    public void Configure(EntityTypeBuilder<BroadcastHistoryLog> builder)
    {
        builder.ToTable("LogBroadcastHistory");
        
        builder.HasOne(h => h.BroadcastSession)
               .WithMany()
               .HasForeignKey(h => h.BroadcastSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        // 🚀 [v6.2.2] 시계열 데이터 조회를 위한 복합 인덱스 (오시리스의 인덱싱)
        builder.HasIndex(h => new { h.BroadcastSessionId, h.LogDate });
    }
}