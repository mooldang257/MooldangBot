using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

public class AvatarSettingConfiguration : IEntityTypeConfiguration<AvatarSetting>
{
    public void Configure(EntityTypeBuilder<AvatarSetting> builder)
    {
        builder.ToTable("sys_avatar_settings");
        
        builder.HasOne(a => a.StreamerProfile)
               .WithOne()
               .HasForeignKey<AvatarSetting>(a => a.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(a => a.StreamerProfileId).IsUnique();
    }
}

public class OverlayPresetConfiguration : IEntityTypeConfiguration<OverlayPreset>
{
    public void Configure(EntityTypeBuilder<OverlayPreset> builder)
    {
        builder.ToTable("sys_overlay_presets");
        
        builder.HasOne(o => o.StreamerProfile)
               .WithMany()
               .HasForeignKey(o => o.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(o => o.StreamerProfileId);
    }
}

public class SharedComponentConfiguration : IEntityTypeConfiguration<SharedComponent>
{
    public void Configure(EntityTypeBuilder<SharedComponent> builder)
    {
        builder.ToTable("sys_shared_components");
        
        builder.HasOne(s => s.StreamerProfile)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(s => s.StreamerProfileId);
    }
}

public class PeriodicMessageConfiguration : IEntityTypeConfiguration<PeriodicMessage>
{
    public void Configure(EntityTypeBuilder<PeriodicMessage> builder)
    {
        builder.ToTable("sys_periodic_messages");

        builder.HasOne(m => m.StreamerProfile)
               .WithMany()
               .HasForeignKey(m => m.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.StreamerProfileId);
    }
}