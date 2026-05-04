using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

// [v11.1] 천상의 장부 매핑 설정 - 포인트 트랜잭션 내역
public class PointTransactionHistoryConfiguration : IEntityTypeConfiguration<LogPointTransactions>
{
    public void Configure(EntityTypeBuilder<LogPointTransactions> builder)
    {
        
        builder.HasOne(p => p.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(p => p.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// [v11.1] 천상의 장부 매핑 설정 - 포인트 일일 요약
public class PointDailySummaryConfiguration : IEntityTypeConfiguration<LogPointDailySummaries>
{
    public void Configure(EntityTypeBuilder<LogPointDailySummaries> builder)
    {
    }
}

// [v11.1] 천상의 장부 매핑 설정 - 룰렛 통계 집계
public class RouletteStatsAggregatedConfiguration : IEntityTypeConfiguration<LogRouletteStats>
{
    public void Configure(EntityTypeBuilder<LogRouletteStats> builder)
    {
    }
}

// [v11.1] 천상의 장부 매핑 설정 - 채팅 상호작용 로그
public class ChatInteractionLogConfiguration : IEntityTypeConfiguration<LogChatInteractions>
{
    public void Configure(EntityTypeBuilder<LogChatInteractions> builder)
    {
        
        builder.HasOne(c => c.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(c => c.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// [v7.0] 후원 내역 (장부)
public class ViewerDonationHistoryConfiguration : IEntityTypeConfiguration<FuncViewerDonationHistories>
{
    public void Configure(EntityTypeBuilder<FuncViewerDonationHistories> builder)
    {
        
        builder.HasOne(v => v.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(v => v.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasOne(v => v.CoreGlobalViewers)
               .WithMany()
               .HasForeignKey(v => v.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
