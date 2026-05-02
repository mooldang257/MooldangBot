using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

// [v7.0] Wallet Architecture 분산화 엔티티 - 시청자 포인트 지갑
public class ViewerPointConfiguration : IEntityTypeConfiguration<ViewerPoint>
{
    public void Configure(EntityTypeBuilder<ViewerPoint> builder)
    {
        builder.ToTable("FuncViewerPoints");
        
        builder.HasOne(v => v.StreamerProfile)
               .WithMany()
               .HasForeignKey(v => v.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasOne(v => v.GlobalViewer)
               .WithMany()
               .HasForeignKey(v => v.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        // [v18.2] UPSERT 무결성을 위한 복합 유니크 인덱스 추가
        builder.HasIndex(v => new { v.StreamerProfileId, v.GlobalViewerId }).IsUnique();
    }
}

// [v7.0] Wallet Architecture 분산화 엔티티 - 시청자 후원 상태/지갑
public class ViewerDonationConfiguration : IEntityTypeConfiguration<ViewerDonation>
{
    public void Configure(EntityTypeBuilder<ViewerDonation> builder)
    {
        builder.ToTable("FuncViewerDonations");
        
        builder.HasOne(v => v.StreamerProfile)
               .WithMany()
               .HasForeignKey(v => v.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasOne(v => v.GlobalViewer)
               .WithMany()
               .HasForeignKey(v => v.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}