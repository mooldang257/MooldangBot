🛠️ 정규화 설계 포인트
ChzzkUid 제거: 무거운 문자열 대신 StreamerProfile 테이블의 정수형 기본키인 StreamerProfileId로 교체합니다.

Category & FeatureType 제거: 이 두 필드는 이미 Master_CommandFeature 마스터 테이블에 정의되어 있으므로, 외래 키인 MasterCommandFeatureId로 대체하여 진정한 **제3정규형(3NF)**을 달성합니다.

복합 인덱스 변경: 기존 (ChzzkUid, Keyword) 인덱스를 (StreamerProfileId, Keyword) 정수+문자열 조합으로 변경하여 MariaDB의 B-Tree 인덱스 검색 속도를 극대화합니다.

1. 수정된 UnifiedCommand.cs (엔티티 코드)
C#
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [파로스의 통합 - v4.3 정규화]: 시스템의 모든 유료/무료 명령어를 통합 관리하는 엔티티입니다.
/// </summary>
// 🔐 [정규화] ChzzkUid 문자열 대신 StreamerProfileId(Int) 기반으로 유니크 인덱스 생성
[Index(nameof(StreamerProfileId), nameof(Keyword), IsUnique = true)]
public class UnifiedCommand
{
    [Key]
    public int Id { get; set; }

    // ----------------------------------------------------
    // [정규화 영역 1] Streamer 연결
    // ----------------------------------------------------
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    // ----------------------------------------------------
    // [정규화 영역 2] 마스터 기능 연결 (Category와 FeatureType 대체)
    // ----------------------------------------------------
    [Required]
    public int MasterCommandFeatureId { get; set; }

    [ForeignKey(nameof(MasterCommandFeatureId))]
    public virtual Master_CommandFeature? MasterFeature { get; set; }

    // ----------------------------------------------------
    // 명령어 고유 속성 (스트리머별 커스텀 설정)
    // ----------------------------------------------------
    [Required]
    [MaxLength(50)]
    public string Keyword { get; set; } = string.Empty;

    public int Cost { get; set; } = 0;

    [Required]
    public CommandCostType CostType { get; set; } 

    [MaxLength(1000)]
    public string ResponseText { get; set; } = string.Empty;

    public int? TargetId { get; set; }

    [Required]
    public CommandRole RequiredRole { get; set; } = CommandRole.Viewer;

    public bool IsActive { get; set; } = true;

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    
    public KstClock? UpdatedAt { get; set; }
}
2. AppDbContext.cs 매핑 및 Fluent API 설정
DB Context에서 해당 엔티티들이 어떻게 관계를 맺고 지워지는지, 그리고 글로벌 필터는 어떻게 적용할지 정의합니다. OnModelCreating 내부에 아래 로직을 반영해야 합니다.

C#
// AppDbContext.cs 내부 OnModelCreating 메서드

modelBuilder.Entity<UnifiedCommand>(entity => 
{
    entity.ToTable("unifiedcommands");

    // 1. 스트리머 삭제 시 해당 채널의 명령어도 연쇄 삭제 (Cascade)
    entity.HasOne(c => c.StreamerProfile)
          .WithMany()
          .HasForeignKey(c => c.StreamerProfileId)
          .OnDelete(DeleteBehavior.Cascade);

    // 2. 마스터 데이터(기능) 삭제 시 명령어 데이터 보호 (Restrict)
    // 마스터 데이터는 함부로 지워지면 안 되므로 제한을 겁니다.
    entity.HasOne(c => c.MasterFeature)
          .WithMany()
          .HasForeignKey(c => c.MasterCommandFeatureId)
          .OnDelete(DeleteBehavior.Restrict);

    // 3. 글로벌 쿼리 필터 적용 (현재 인증된 스트리머의 명령어만 조회되도록 격리)
    entity.HasQueryFilter(e => !_userSession.IsAuthenticated || 
                               e.StreamerProfile!.ChzzkUid == _userSession.ChzzkUid);
});
3. 무손실 마이그레이션 적용 가이드 (Migration Strategy)
이 구조 변경을 바로 적용(dotnet ef database update)하면, 기존 ChzzkUid와 Category, FeatureType으로 저장되어 있던 기존 데이터가 유실되거나 제약 조건 오류가 발생할 수 있습니다.

따라서 EF Core 마이그레이션 파일의 Up() 메서드 내부에 다음 SQL을 삽입하여 데이터를 이관해야 합니다.

마이그레이션 적용 시퀀스:

신규 컬럼 StreamerProfileId와 MasterCommandFeatureId를 Nullable(int?)로 먼저 생성합니다.

migrationBuilder.Sql()을 사용해 기존 데이터를 기반으로 FK 값을 찾아 업데이트합니다.

SQL
-- 1. StreamerProfileId 매핑
UPDATE unifiedcommands uc
JOIN streamerprofiles sp ON uc.ChzzkUid = sp.ChzzkUid
SET uc.StreamerProfileId = sp.Id;

-- 2. MasterCommandFeatureId 매핑
-- (기존 Enum Category의 int값과 FeatureType 문자열을 기반으로 Master 테이블과 조인하여 업데이트)
UPDATE unifiedcommands uc
JOIN master_commandfeatures mcf 
  ON mcf.CategoryId = uc.Category AND mcf.TypeName = uc.FeatureType
SET uc.MasterCommandFeatureId = mcf.Id;
업데이트가 완료되면 StreamerProfileId와 MasterCommandFeatureId 컬럼을 NOT NULL 제약조건으로 변경합니다.

마지막으로 기존 ChzzkUid, Category, FeatureType 컬럼을 드롭(DropColumn)합니다.