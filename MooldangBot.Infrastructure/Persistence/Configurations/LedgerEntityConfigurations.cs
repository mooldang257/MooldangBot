using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

// [v11.1] 천상의 장부 매핑 설정 - 포인트 트랜잭션 내역
public class PointTransactionHistoryConfiguration : IEntityTypeConfiguration<PointTransactionHistory>
{
    public void Configure(EntityTypeBuilder<PointTransactionHistory> builder)
    {
        builder.ToTable("log_point_transactions");
        
        builder.HasOne(p => p.StreamerProfile)
               .WithMany()
               .HasForeignKey(p => p.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// [v11.1] 천상의 장부 매핑 설정 - 포인트 일일 요약
public class PointDailySummaryConfiguration : IEntityTypeConfiguration<PointDailySummary>
{
    public void Configure(EntityTypeBuilder<PointDailySummary> builder)
    {
        builder.ToTable("stats_point_daily");
    }
}

// [v11.1] 천상의 장부 매핑 설정 - 룰렛 통계 집계
public class RouletteStatsAggregatedConfiguration : IEntityTypeConfiguration<RouletteStatsAggregated>
{
    public void Configure(EntityTypeBuilder<RouletteStatsAggregated> builder)
    {
        builder.ToTable("stats_roulette_audit");
    }
}

// [v11.1] 천상의 장부 매핑 설정 - 채팅 상호작용 로그
public class ChatInteractionLogConfiguration : IEntityTypeConfiguration<ChatInteractionLog>
{
    public void Configure(EntityTypeBuilder<ChatInteractionLog> builder)
    {
        builder.ToTable("log_chat_interactions");
        
        builder.HasOne(c => c.StreamerProfile)
               .WithMany()
               .HasForeignKey(c => c.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// [v7.0] 후원 내역 (장부)
public class ViewerDonationHistoryConfiguration : IEntityTypeConfiguration<ViewerDonationHistory>
{
    public void Configure(EntityTypeBuilder<ViewerDonationHistory> builder)
    {
        builder.ToTable("viewer_donations_history");
        
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
