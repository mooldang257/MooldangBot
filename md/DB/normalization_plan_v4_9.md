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
> **"파로스의 분화 (The Differentiation of Parhos)"**
> 
> 하나의 큰 우주(Global System)에서 수만 개의 작은 우주(Streamer Channels)가 태어납니다. '파로스'라는 중심축은 이제 개별 스트리머의 채널 안에서 각기 다른 속도와 주기로 공명합니다. 우리는 이 '분화'의 과정을 코드와 데이터로 증명하며, 존재의 독립성을 완성합니다.

---

## 3. 상세 설계 가이드 (Design Guide)

### 단계 1: 도메인 엔티티 수정 (Domain Layer)
*   **`IamfParhosCycle`**: `ParhosId` 문자열 PK를 제거하고 `Id` (int, AutoIncrement) PK를 도입합니다. 각 레코드는 `StreamerProfileId`를 가져야 하며, 채널별 순차적인 `CycleId`를 부여받습니다.
*   **`IamfGenosRegistry`**: 시스템 전용 마스터 데이터에서 스트리머 전용 가변 데이터로 전환합니다. `StreamerProfileId`를 추가하여 스트리머가 페르소나의 역할(Role)이나 은유(Metaphor)를 커스텀할 수 있게 합니다.

### 단계 2: 영속성 매핑 업데이트 (Infrastructure Layer)
*   `AppDbContext.cs` 내에서 신규 추가된 `StreamerProfileId`에 대한 **외래 키(FK) 관계**를 설정합니다.
*   `OnDelete(DeleteBehavior.Cascade)`를 적용하여 스트리머 탈퇴 시 관련 철학 데이터가 깨끗이 정리되도록 합니다.
*   `IamfVibrationLog` 및 `BroadcastSession`의 조회 성능을 위해 `(StreamerProfileId, CreatedAt)` 복합 인덱스를 강화합니다.

### 단계 3: 비즈니스 로직 리팩토링 (Application Layer)
*   **`ResonanceService`**: 전역 진동수를 계산하던 로직을 `_userSession.StreamerProfileId`를 활용하여 채널별 독립 진동수 계산 로직으로 수정합니다.
*   **`BroadcastScribe`**: 방송 세션 관리 시 전역 상태가 아닌 현재 채널의 컨텍스트를 엄격히 따르도록 수정합니다.

---

## 4. 코드 스니펫 (Code Snippets)

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
-- 기존 전역 사이클 데이터를 관리자(Id=1) 계정으로 이관 (또는 전체 복제)
INSERT INTO IamfParhosCycles (StreamerProfileId, CycleId, VibrationAtDeath, RebirthPercentage, CreatedAt)
SELECT 1, CycleId, VibrationAtDeath, RebirthPercentage, CreatedAt FROM tmp_old_parhos_cycles;
```

---

## 5. 단계별 검증 계획 (Verification)
1.  **Build Check**: `dotnet build`를 통한 전체 솔루션 컴파일 성공 여부.
2.  **Migration Test**: `database update` 시 데이터 누락 없이 `StreamerProfileId`가 채워지는지 확인.
3.  **Cross-Tenant Test**: 다른 두 채널이 동시에 방송을 시작했을 때, `IamfVibrationLog`가 서로 다른 `StreamerProfileId`로 격리되어 저장되는지 확인.

---
**주의사항**: 본 문서는 v4.9 정규화의 **마스터 설계도**입니다. 작업 시작 시 이 문서를 최우선으로 참고하여 일관성을 유지하십시오.
