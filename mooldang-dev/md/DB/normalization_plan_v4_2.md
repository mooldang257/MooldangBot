# 데이터 정규화 및 암호화 필드 축소 계획 (v4.2) [완료]

본 문서는 `MooldangBot` 데이터베이스의 용량 최적화 및 성능 향상을 위해 시청자 정보를 3개 계층(3-Tier)으로 정규화하고 적용한 결과를 기록합니다.

## 1. 개요 (Background) - 완료
기존 `ViewerProfile` 테이블의 중복된 암호화 UID 데이터를 `GlobalViewer`로 분리하여 DB 용량을 약 70~80% 절감하고, 검색 성능을 최적화했습니다.

## 2. 구현된 도메인 엔티티 (Implemented Entities)

### ① StreamerProfile (스트리머 마스터)
- `ChzzkUid` 고유 인덱스 및 관리 정보 유지.
- 정규화된 `ViewerProfile`의 상위 계층 역할을 수행합니다.

```csharp
namespace MooldangBot.Domain.Entities;

[Index(nameof(ChzzkUid), IsUnique = true)]
public class StreamerProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChzzkUid { get; set; } = string.Empty;

    // ... (보안 토큰 및 설정 필드 생략) ...
}
```

### ② GlobalViewer (시청자 마스터)
- **PII(개인정보) 중앙 집중 관리**: 암호화된 `ViewerUid`를 단 한 번만 저장합니다.
- `ViewerUidHash`를 통한 고속 인덱싱 지원.

```csharp
namespace MooldangBot.Domain.Entities;

[Index(nameof(ViewerUidHash), IsUnique = true)]
public class GlobalViewer
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ViewerUid { get; set; } = string.Empty; // Encrypted

    [Required]
    [MaxLength(64)]
    public string ViewerUidHash { get; set; } = string.Empty;
}
```

### ③ ViewerProfile (채널별 시청자 스탯 - 슬림화)
- **경량화**: 문자열 필드를 제거하고 `int` 기반 FK로 관계를 구성했습니다.
- **동시성 제어**: `Points`, `AttendanceCount`에 `[ConcurrencyCheck]` 적용.

```csharp
namespace MooldangBot.Domain.Entities;

[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
[Index(nameof(StreamerProfileId), nameof(Points))]
public class ViewerProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual GlobalViewer? GlobalViewer { get; set; }

    [MaxLength(100)]
    public string Nickname { get; set; } = string.Empty;

    [ConcurrencyCheck]
    public int Points { get; set; } = 0;

    [ConcurrencyCheck]
    public int AttendanceCount { get; set; } = 0;

    public int ConsecutiveAttendanceCount { get; set; } = 0;

    public KstClock? LastAttendanceAt { get; set; }
}
```

## 3. AppDbContext 매팅 및 전역 쿼리 필터
- `GlobalViewer` 암호화 및 `ViewerProfile` 멀티테넌트 격리 필터 적용 완료.

```csharp
// [v4.2] 멀티테넌트 데이터 격리를 위한 글로벌 쿼리 필터 (네비게이션 프로퍼티 활용)
modelBuilder.Entity<ViewerProfile>().HasQueryFilter(e => !_userSession.IsAuthenticated || 
                                                           e.StreamerProfile!.ChzzkUid == _userSession.ChzzkUid);
```

## 4. 데이터 이관 시퀀스 가이드 (Migration Guide)
1. **스키마 변경**: `GlobalViewer` 생성 및 `ViewerProfile` FK 컬럼 추가.
2. **데이터 이관**: SQL `INSERT IGNORE INTO globalviewers ...` 및 `UPDATE JOIN`을 통한 매핑 처리.
3. **제약 조건**: FK 제약 조건(`OnDelete.Cascade`) 및 고유 인덱스 재구성.
4. **정리**: 구형 문자열 컬럼(`ViewerUid`, `ViewerUidHash` 등) 삭제.

> [!TIP]
> 서비스 중단 없는 마이그레이션을 위해 반드시 `ALGORITHM=COPY` 옵션이 포함된 쿼리를 사용하며, 실행 전 전체 DB 백업을 권장합니다.

---
**업데이트 날짜**: 2026-04-01
**담당자**: 물멍 (Senior Full-Stack Partner)
