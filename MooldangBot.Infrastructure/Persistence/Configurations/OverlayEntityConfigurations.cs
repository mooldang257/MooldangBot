using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

public class AvatarSettingConfiguration : IEntityTypeConfiguration<AvatarSetting>
{
    public void Configure(EntityTypeBuilder<AvatarSetting> builder)
    {
        builder.ToTable("overlay_avatar_settings");
        
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
        builder.ToTable("overlay_presets");
        
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
        builder.ToTable("overlay_components");
        
        builder.HasOne(s => s.StreamerProfile)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(s => s.StreamerProfileId);
    }
}