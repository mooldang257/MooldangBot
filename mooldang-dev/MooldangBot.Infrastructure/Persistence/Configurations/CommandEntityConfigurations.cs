using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;
using MooldangBot.Infrastructure.Sagas;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

// [파로스의 통합]: UnifiedCommand 설정 (v4.3 정형화 적용)
public class UnifiedCommandConfiguration : IEntityTypeConfiguration<UnifiedCommand>
{
    public void Configure(EntityTypeBuilder<UnifiedCommand> builder)
    {
        builder.ToTable("FuncCmdUnified");

        builder.Property(e => e.Keyword)
               // 🔍 대소문자 무관 검색을 위한 명시적 Collation 설정 (Osiris)
               .UseCollation("utf8mb4_unicode_ci"); 

        builder.Property(e => e.CostType).HasConversion<string>();
        builder.Property(e => e.RequiredRole).HasConversion<string>();
        builder.Property(e => e.MatchType).HasConversion<string>();

        // 마스터 데이터 연동 제거 (Enum 기반으로 관리됨)
        builder.Property(e => e.FeatureType)
               .HasConversion<string>();

        builder.Property(e => e.Priority).HasDefaultValue(0);

        // 1. 스트리머 삭제 시 해당 채널의 명령어도 연쇄 삭제 (Cascade)
        builder.HasOne(c => c.StreamerProfile)
               .WithMany()
               .HasForeignKey(c => c.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        // [Index] 복합 인덱스: (StreamerProfileId, Keyword, IsActive, IsDeleted) - 지능형 매칭 최적화
        builder.HasIndex(e => new { e.StreamerProfileId, e.Keyword, e.IsActive, e.IsDeleted })
               .HasDatabaseName("IX_Command_Search");

        builder.HasIndex(e => new { e.StreamerProfileId, e.TargetId });

        // 🚀 [Phase 2] 커서 기반 페이지네이션 최적화
        builder.HasIndex(e => new { e.StreamerProfileId, e.Id })
               .IsDescending(false, true)
               .HasDatabaseName("IX_UnifiedCommand_CursorPaging");
    }
}

// [v11.1] 천상의 장부 매핑 설정 중 명령어 실행 로그
public class CommandExecutionLogConfiguration : IEntityTypeConfiguration<CommandExecutionLog>
{
    public void Configure(EntityTypeBuilder<CommandExecutionLog> builder)
    {
        builder.ToTable("LogCommandExecutions");

        builder.HasOne(c => c.StreamerProfile)
               .WithMany()
               .HasForeignKey(c => c.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// 🧠 [v6.0] 자율 복구 신경망: Saga State Machine 영속성 매핑
public class CommandExecutionSagaStateConfiguration : IEntityTypeConfiguration<CommandExecutionSagaState>
{
    public void Configure(EntityTypeBuilder<CommandExecutionSagaState> builder)
    {
        builder.ToTable("SysSagaCommandExecutions");
        
        // 추적 유전자를 PK로 사용
        builder.HasKey(e => e.CorrelationId); 
        
        // [이지스의 파수꾼]: 상태 및 생애 주기 최적화 인덱싱
        builder.HasIndex(e => e.CurrentState);
        builder.HasIndex(e => e.CreatedAt);
    }
}