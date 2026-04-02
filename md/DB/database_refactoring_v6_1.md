# MooldangBot v6.1 데이터베이스 정규화 및 고도화 명세 (Master Plan)

이 문서는 MooldangBot의 척추인 데이터베이스를 기술 헌법 v6.0에 맞춰 물리적인 레벨에서 정규화하고, 대규모 트래픽 환경에서의 무결성을 보장하기 위한 **전략적 리팩토링 계획**을 담고 있습니다.

---

## 🐶 핵심 철학: 정체성의 공명 (Resonance of Identity)
- **무결성(Integrity)**: 수만 번 의 진동(이벤트) 속에서도 데이터는 훼손되지 않아야 합니다.
- **투명성(Transparency)**: 물리적 삭제가 아닌 논리적 보존(IAMF)을 통해 모든 존재의 이력을 추적합니다.
- **효율성(Efficiency)**: "바퀴를 다시 발명하지 마라." 검증된 라이브러리를 통해 최적의 기틀을 만듭니다.

---

## 🚀 [Priority 1: Critical] 무결성 및 리눅스 호환성 완성

### 1. EFCore.NamingConventions를 통한 스네이크 케이스 자동화
**설계 목적**: 직접 리플렉션 루프를 도는 대신, 검증된 오픈소스 패키지를 사용하여 외래 키, 인덱스 등 모든 매핑 명칭을 MariaDB 표준(`snake_case`)으로 자동화합니다.

```csharp
// DbContext 설정 시 (Infrastructure 계층)
options.UseMariaDb(connectionString)
       .UseSnakeCaseNamingConvention(); // [v6.1] 전역 스네이크 케이스 자동화
```

### 2. MariaDB 친화적 동시성 제어 (Concurrency Token)
**설계 목적**: `byte[]` 기반의 `RowVersion` 대신, MariaDB 생태계의 표준인 `int` 필드 직접 체크 및 `DateTime` 기반 토큰을 사용하여 레이스 컨디션을 방지합니다.

```csharp
public class ViewerProfile : IAuditable
{
    public int Id { get; set; }
    
    // [보강] 포인트 등 크리티컬한 수치에 대해 직접 동시성 체크
    [ConcurrencyCheck]
    public int Points { get; set; }
    
    // [v6.1] MariaDB 표준: 업데이트 시점을 동시성 토큰으로 활용
    [ConcurrencyCheck]
    public KstClock? UpdatedAt { get; set; }
}
```

---

## 🏹 [Priority 2: High] '존재의 보존' (IAMF) 표준화

### 3. 소프트 딜리트 인터페이스 통합 (ISoftDeletable)
**설계 목적**: 파편화된 `DelYn` (string) 스타일을 .NET 표준인 **`bool IsDeleted`**로 통합하고 전역 쿼리 필터를 강제합니다.

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; } // [v6.1] 레거시 DelYn 대체
    KstClock? DeletedAt { get; set; }
}

// AppDbContext 전역 필터 (Osiris standard)
modelBuilder.Entity<StreamerProfile>().HasQueryFilter(e => !e.IsDeleted);
```

---

## 📂 리팩토링 로드맵 (Roadmap)
1. **[x] v6.1.1**: `EFCore.NamingConventions` 의존성 추가 및 `DbContext` 적용. (완료 ⚓)
2. **[x] v6.1.2**: `ISoftDeletable` 인터페이스 정의 및 주요 엔터티 필드 타입 전환 완료. (완료 🚢)
3. **[/] v6.1.3**: `AppDbContext` 전역 필터 및 감사 로그 자동화 적용. (진행 중 🔍)
4. **[ ] v6.1.4**: 마이그레이션 생성 및 **데이터 유실 방지 SQL** 적용을 통한 **'데이터 정규화'** 완료.

> [!IMPORTANT]
> **마이그레이션 안전장치**: `Up` 메서드 내에서 `AlterColumn` 실행 전 반드시 아래 SQL을 통해 기존 데이터를 변환해야 합니다.
> ```csharp
> migrationBuilder.Sql("UPDATE streamer_profiles SET del_yn = '1' WHERE del_yn = 'Y';");
> migrationBuilder.Sql("UPDATE streamer_profiles SET del_yn = '0' WHERE del_yn = 'N';");
> ```

---
**아키텍트의 결론**: 이 리팩토링은 MariaDB 환경에서의 정합성을 100% 보장하며, **봇(Bot)**이 거대한 파도를 견디며 항해할 수 있는 **'강철 용골'**을 다지는 작업입니다. ⚓🚢🦾✨
