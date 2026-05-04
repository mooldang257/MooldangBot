using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

public class AvatarSettingConfiguration : IEntityTypeConfiguration<SysAvatarSettings>
{
    public void Configure(EntityTypeBuilder<SysAvatarSettings> builder)
    {
        
        builder.HasOne(a => a.CoreStreamerProfiles)
               .WithOne()
               .HasForeignKey<SysAvatarSettings>(a => a.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(a => a.StreamerProfileId).IsUnique();
    }
}

public class OverlayPresetConfiguration : IEntityTypeConfiguration<SysOverlayPresets>
{
    public void Configure(EntityTypeBuilder<SysOverlayPresets> builder)
    {
        
        builder.HasOne(o => o.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(o => o.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(o => o.StreamerProfileId);
    }
}

public class SharedComponentConfiguration : IEntityTypeConfiguration<SysSharedComponents>
{
    public void Configure(EntityTypeBuilder<SysSharedComponents> builder)
    {
        
        builder.HasOne(s => s.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(s => s.StreamerProfileId);
    }
}

public class PeriodicMessageConfiguration : IEntityTypeConfiguration<SysPeriodicMessages>
{
    public void Configure(EntityTypeBuilder<SysPeriodicMessages> builder)
    {

        builder.HasOne(m => m.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(m => m.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.StreamerProfileId);
    }
}