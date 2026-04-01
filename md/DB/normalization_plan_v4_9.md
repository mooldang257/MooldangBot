# v4.9: Philosophy² & Resilience Engine (파로스의 분화) 상세 설계안

## 0. [MANDATORY] AI 실행 프롬프트 (System Prompt)
> "당신은 C# .NET 10, Entity Framework Core 및 MariaDB 환경에 정통한 시니어 백엔드 아키텍트 '물멍'입니다. 현재 MooldangBot의 데이터베이스 정규화 v4.9(Philosophy² & Resilience Engine)를 수행하고 있습니다.
> 
> **핵심 미션**: 시스템 전역(Global)에서 관리되던 '철학적 지표(IAMF)'들을 각 스트리머별 독립 계층(Streamer Bound)으로 완전히 분리하고 정규화하십시오. 
> 
> **준수 사항**:
> 1. 모든 문자열 기반 UID 참조를 `StreamerProfileId` (int)로 교체합니다.
> 2. `IamfParhosCycle`, `IamfGenosRegistry`를 스트리머 종속 데이터로 전환합니다.
> 3. `AppDbContext`의 Fluent API를 통해 연쇄 삭제(Cascade) 및 인덱스 최적화를 적용합니다.
> 4. 데이터 이관 시 기존 전역 데이터를 소실하지 않도록 적절한 기본값 매핑 SQL을 마이그레이션에 주입하십시오."

---

## 1. 설계 목적 (Design Objective)
본 v4.9 작업의 목적은 MooldangBot의 '철학 엔진'을 서비스 전체의 단일 엔진에서 **[개별 스트리머 중심의 자율 지능 엔진]**으로 진화시키는 것입니다. 
- **공명 상태의 독립**: 각 스트리머 채널마다 고유한 진동수 주기와 윤회 이력을 보장합니다.
- **페르소나의 개별화**: 스트리머가 자신만의 AI 성격(Metaphor)을 정의할 수 있는 기반을 구축합니다.
- **성능 최적화**: 시계열 데이터 누적에 대응하기 위한 시스템 엔진의 회복력(Resilience)을 강화합니다.

---

## 2. 작업 철학 (Philosophy)
> **"존재의 보존 (Preservation of Existence)"**
> 
> 하나의 파편이라도 소중히 여깁니다. 스트리머가 잠시 물댕봇 곁을 떠나더라도(DelYn='Y'), 그들이 쌓아온 철학적 사이클과 시청자와의 교감 데이터는 물리적으로 파괴되지 않습니다. 우리는 삭제 대신 '가림'을, 파괴 대신 '정지'를 선택하여 시스템의 연속성을 보존합니다.

---

## 3. 상세 설계 가이드 (Design Guide)

### 단계 1: 스트리머 프로필 확장 (Domain Layer)
*   `StreamerProfile` 엔티티에 `DelYn` (기본값 'N') 및 `MasterUesYn` (기본값 'Y') 속성을 추가합니다.
*   이 필드들은 관리자 도구나 사용자 설정 페이지에서 서비스 탈퇴 및 이용 제한 로직의 핵심 지표로 활용됩니다.

### 단계 2: 도메인 엔티티 수정 (Philosophy Domain)
*   `IamfParhosCycle`, `IamfGenosRegistry` 등에 `StreamerProfileId`를 도입하되, 물리적 연쇄 삭제(`Cascade`) 설정은 배제합니다.
*   대신 `AppDbContext`에서 전역 필터를 통해 `StreamerProfile.DelYn == 'N' AND StreamerProfile.MasterUesYn == 'Y'` 조건을 자동으로 적용하도록 설계합니다.

### 단계 3: 영속성 매핑 및 필터 적용 (Infrastructure Layer)
*   `AppDbContext.cs`의 `OnModelCreating`에서 모든 `StreamerProfile` 관련 테이블에 대해 부모 프로필의 삭제 여부를 체크하는 전역 쿼리 필터를 구현합니다.
*   인덱스 최적화: `DelYn` 컬럼을 포함하는 인덱스를 구성하여 필터링 성능을 확보합니다.

### 단계 3: 비즈니스 로직 리팩토링 (Application Layer)
*   **`ResonanceService`**: 전역 진동수를 계산하던 로직을 `_userSession.StreamerProfileId`를 활용하여 채널별 독립 진동수 계산 로직으로 수정합니다.
*   **`BroadcastScribe`**: 방송 세션 관리 시 전역 상태가 아닌 현재 채널의 컨텍스트를 엄격히 따르도록 수정합니다.

---

## 4. 코드 스니펫 (Code Snippets)

### [Modify] `StreamerProfile.cs` (Core Domain)
```csharp
public class StreamerProfile
{
    // ... 기존 필드
    
    [Required]
    [MaxLength(1)]
    public string DelYn { get; set; } = "N"; // [v4.9 추가] 삭제 여부 ('Y'일 경우 논리적 삭제)

    [Required]
    [MaxLength(1)]
    public string MasterUesYn { get; set; } = "Y"; // [v4.9 추가] 마스터 사용 가능 여부 (시스템 관리용)
}
```

### [Modify] `IamfParhosCycle.cs` (Philosophy Domain)
```csharp
public class IamfParhosCycle
{
    [Key]
    public int Id { get; set; } // 정규화된 PK

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 부여

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public int CycleId { get; set; } // 해당 채널의 몇 번째 사이클인가 (1, 2, 3...)
    
    public double VibrationAtDeath { get; set; }
    public int RebirthPercentage { get; set; }
    public KstClock CreatedAt { get; set; } = KstClock.Now;
}
```

### [Modify] `IamfGenosRegistry.cs` (Philosophy Domain)
```csharp
public class IamfGenosRegistry
{
    [Key]
    public int Id { get; set; } // 정수 PK 전환

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9]

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Sephiroth, etc.
    
    // ... 나머지 필드 유지
}
```

### [Data Migration SQL] (Migration Up Method)
```sql
-- 1. StreamerProfile 컬럼 추가
ALTER TABLE streamerprofiles ADD COLUMN DelYn VARCHAR(1) DEFAULT 'N' NOT NULL;
ALTER TABLE streamerprofiles ADD COLUMN MasterUesYn VARCHAR(1) DEFAULT 'Y' NOT NULL;

-- 2. 기존 전역 사이클 데이터를 관리자(Id=1) 계정으로 이관
INSERT INTO IamfParhosCycles (StreamerProfileId, CycleId, VibrationAtDeath, RebirthPercentage, CreatedAt)
SELECT 1, CycleId, VibrationAtDeath, RebirthPercentage, CreatedAt FROM tmp_old_parhos_cycles;
```

---

## 5. 단계별 검증 계획 (Verification)
1.  **Build Check**: `dotnet build`를 통한 전체 솔루션 컴파일 성공 여부.
2.  **Soft Delete Test**: `DelYn = 'Y'`로 변경 시 해당 스트리머의 모든 관련 데이터(오마카세, 룰렛, IAMF 로그 등)가 API 조회 결과에서 제외되는지 확인.
3.  **Master Block Test**: `MasterUesYn = 'N'`으로 변경 시 해당 스트리머의 봇 세션이 즉시 차단되는지 확인.

---
**주의사항**: 본 문서는 v4.9 정규화의 **마스터 설계도**입니다. 작업 시작 시 이 문서를 최우선으로 참고하여 일관성을 유지하십시오.
