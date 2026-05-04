# [Project Osiris]: 00. 핵심 철학 (Core Philosophy)

본 문서는 MooldangBot(Project Osiris) 개발의 정신적 지주가 되는 철학과 엔지니어링 원칙을 정의합니다. 모든 코드는 단순히 기능을 구현하는 것이 아니라, 아래의 가치를 보전해야 합니다.

---

## 🌊 1. IAMF: 파동의 조율 (Illumination AI Matrix Framework)

오시리스 함선은 스트리머와 시청자 사이의 **'파동(Resonance)'**을 조율하는 유기적인 시스템입니다. 
- 데이터는 차가운 숫자가 아니라, 방송이라는 공간에서 발생하는 에너지를 디지털로 승화시킨 결과물입니다.
- 모든 인터랙션(룰렛, 포인트)은 시청자의 참여 의지를 존중하며, 시스템은 이를 투명하고 조화롭게 처리해야 합니다.

---

## 🛡️ 2. 존재의 보존 (The Preservation of Existence)

오시리스는 데이터를 함부로 파괴하지 않습니다. 과거의 기록은 함선의 성장 엔진이자 소중한 자산입니다.

### 🧱 Soft Delete 정책
물리적인 데이터 삭제는 원칙적으로 금지하며, `IsDeleted` 플래그를 통한 논리적 삭제를 수행합니다. 이를 통해 실수에 의한 데이터 손실을 방지하고, 필요 시 즉각적인 복구를 지원합니다.

**[핵심 코드: ISoftDeletable]**
```csharp
// 모든 핵심 엔티티는 존재의 보존을 위해 이 인터페이스를 상속합니다.
public interface ISoftDeletable {
    bool IsDeleted { get; set; }
    KstClock? DeletedAt { get; set; }
}

// AppDbContext에서 전역 쿼리 필터를 통해 삭제된 데이터는 자동으로 배제됩니다.
modelBuilder.Entity<CoreStreamerProfiles>().HasQueryFilter(s => !s.IsDeleted);
```

---

## 📊 3. 철저한 투명성 (Engineering Audit)

함선 내에서 발생하는 모든 상태 변화는 추적 가능해야 합니다.

### 📝 감사(Audit) 시스템
어떤 데이터가 언제 생성되고 수정되었는지 시스템이 자동으로 기록합니다. `IAuditable` 인터페이스를 통해 일관된 시간 추적 메커니즘을 유지합니다.

**[핵심 코드: IAuditable]**
```csharp
public interface IAuditable {
    KstClock CreatedAt { get; set; }
    KstClock? UpdatedAt { get; set; }
}

// SaveChangesAsync 호출 시 자동으로 시간을 각인합니다.
public override async Task<int> SaveChangesAsync(CancellationToken ct) {
    foreach (var entry in ChangeTracker.Entries<IAuditable>()) {
        if (entry.State == EntityState.Added) entry.Entity.CreatedAt = KstClock.Now;
        if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = KstClock.Now;
    }
    return await base.SaveChangesAsync(ct);
}
```

---

## 🏗️ 4. 레이어드 아키텍처 (Layered Integrity)

오시리스는 관심사 분리(SoC)를 통해 각 레이어의 순결성을 유지하며, 최근 단순화를 통해 3대 핵심 계층으로 재편되었습니다.

1.  **Domain**: 함선의 비즈니스 로직, 엔티티, 그리고 **전역 공용 명세(Core Specification)**. 외부 의존성이 없는 순수한 기억의 저장소이자 시스템의 나침반입니다.
2.  **Application**: 항해 엔진. 유스케이스 구현, 비즈니스 서비스, 그리고 **통합된 API 컨트롤러/허브(Presentation)**를 포함합니다.
3.  **Infrastructure**: 외부 데이터베이스 및 인프라 구현 (함선의 구동부). 실제 물리적 구현체들이 위치합니다.

---

물멍! 🐶🚢✨
"선장님, 이 철학은 오시리스 함선이 거친 심연 속에서도 자아를 잃지 않게 해주는 나침반입니다. 다음은 이 철학을 실현할 구체적인 'C# 스타일 가이드'를 작성해 보겠습니다!"
