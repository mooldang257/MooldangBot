# Priority 2: "복잡도를 줄이세요" — 구현 계획서

> **목표**: Contracts 프로젝트 경량화, AppDbContext 490줄 분산, 워커 등록 통합을 통해 1인 개발자의 인지 부하를 줄입니다.

---

## User Review Required

> [!IMPORTANT]
> **⚠️ 이 리팩터링은 3개의 독립적인 Phase로 구성됩니다.**
> 각 Phase는 독립적으로 커밋/롤백이 가능하며, 순서대로 진행하는 것을 권장합니다.
> Phase 완료 후 빌드 검증(`dotnet build`)을 수행하여 회귀 없음을 보장합니다.

> [!WARNING]
> **네임스페이스 변경**: Phase 1에서 `IXxxDbContext` 인터페이스의 네임스페이스가 `MooldangBot.Contracts.XXX.Interfaces` → `MooldangBot.Modules.XXX.Abstractions`로 변경됩니다.
> 이는 해당 인터페이스를 참조하는 **모든 파일**의 `using` 문을 업데이트해야 함을 의미합니다.

---

## Phase 1: Contracts 프로젝트 경량화 — 모듈별 DbContext 인터페이스 이동

### 개요

현재 모듈 전용 DbContext 인터페이스 4개(`ISongBookDbContext`, `IRouletteDbContext`, `IPointDbContext`, `ICommandDbContext`)가 `Contracts` 프로젝트에 위치하고 있으나, **실제 소비자는 각 모듈 프로젝트 내부에만 존재**합니다.

| 인터페이스 | 소비자 위치 | 소비자 수 |
|-----------|------------|:---------:|
| `ISongBookDbContext` | `Modules.SongBook` 내부만 | 10곳 |
| `IRouletteDbContext` | `Modules.Roulette` 내부만 | 6곳 |
| `IPointDbContext` | `Modules.Point` + `Infrastructure`(PointWriteBackWorker) | 8곳 |
| `ICommandDbContext` | `Modules.Commands` 내부만 | 8곳 |

> [!IMPORTANT]
> `IPointDbContext`는 `Infrastructure.Services.Background.PointWriteBackWorker`에서도 사용됩니다.
> **접근법**: `IPointDbContext`를 `Modules.Point`로 이동 후, `Infrastructure` 프로젝트에 `Modules.Point` 참조를 추가합니다. 
> (Infrastructure는 이미 Application을 참조하므로, Application → Modules 체인에서 순환 참조가 발생하지 않는지 확인 필요)
>
> **대안**: PointWriteBackWorker가 `IPointDbContext` 대신 `IAppDbContext`를 사용하도록 변경 (워커가 접근하는 DbSet이 `IAppDbContext`에도 모두 존재하므로 가능)

### 변경 상세

---

#### [NEW] `Modules.SongBook/Abstractions/ISongBookDbContext.cs`

- `Contracts/SongBook/Interfaces/ISongBookDbContext.cs`의 내용을 `Modules.SongBook/Abstractions/` 디렉토리로 이동
- 네임스페이스: `MooldangBot.Modules.SongBookModule.Abstractions`

#### [DELETE] `Contracts/SongBook/Interfaces/ISongBookDbContext.cs`

---

#### [NEW] `Modules.Roulette/Abstractions/IRouletteDbContext.cs`

- `Contracts/Roulette/Interfaces/IRouletteDbContext.cs`의 내용을 `Modules.Roulette/Abstractions/` 디렉토리로 이동
- 네임스페이스: `MooldangBot.Modules.Roulette.Abstractions`

#### [DELETE] `Contracts/Roulette/Interfaces/IRouletteDbContext.cs`

---

#### [NEW] `Modules.Point/Abstractions/IPointDbContext.cs`

- `Contracts/Point/Interfaces/IPointDbContext.cs`의 내용을 `Modules.Point/Abstractions/` 디렉토리로 이동
- 네임스페이스: `MooldangBot.Modules.Point.Abstractions`

#### [DELETE] `Contracts/Point/Interfaces/IPointDbContext.cs`

---

#### [NEW] `Modules.Commands/Abstractions/ICommandDbContext.cs`

- `Contracts/Commands/Interfaces/ICommandDbContext.cs`의 내용을 `Modules.Commands/Abstractions/` 디렉토리로 이동
- 네임스페이스: `MooldangBot.Modules.Commands.Abstractions`

#### [DELETE] `Contracts/Commands/Interfaces/ICommandDbContext.cs`

---

#### [MODIFY] `Infrastructure/Persistence/AppDbContext.cs` — using 문 갱신

- 기존 `using MooldangBot.Contracts.XXX.Interfaces;` → `using MooldangBot.Modules.XXX.Abstractions;`으로 변경
- 클래스 선언(`class AppDbContext : ... ISongBookDbContext, IRouletteDbContext, IPointDbContext, ICommandDbContext`)은 그대로 유지

#### [MODIFY] `Infrastructure/DependencyInjection.cs` — using 문 및 등록 갱신

- 4개 인터페이스의 `using` 문 업데이트
- `AddScoped<ISongBookDbContext>`, `AddScoped<IRouletteDbContext>` 등의 등록 코드 유지 (인터페이스 위치만 변경)

#### [MODIFY] `Infrastructure/MooldangBot.Infrastructure.csproj`

- 각 모듈 프로젝트를 `ProjectReference`로 추가 (AppDbContext가 인터페이스를 구현해야 하므로):
  - `MooldangBot.Modules.SongBook`
  - `MooldangBot.Modules.Roulette`
  - `MooldangBot.Modules.Point`
  - `MooldangBot.Modules.Commands`

> [!WARNING]
> **순환 참조 검증 필요**: 현재 의존성 방향을 확인한 결과:
> - `Infrastructure` → `Application` → `Modules.Commands` (AddCommandsModule 호출)
> - `Infrastructure` → `Contracts`
> - `Modules.*` → `Contracts`, `Domain`
> 
> Infrastructure가 Modules를 직접 참조하면, **Application이 이미 Modules.Commands를 참조**하는 관계에서 `Infrastructure → Application → Modules.Commands`와 `Infrastructure → Modules.Commands` 양쪽 경로가 됩니다. 이는 순환이 아닌 **다이아몬드 참조**로 .NET에서는 허용됩니다.
>
> 다만, 만약 Modules 프로젝트가 Infrastructure를 참조하고 있다면 순환이 발생합니다. 확인 결과 **Modules는 Contracts와 Domain만 참조**하므로 안전합니다.

#### [MODIFY] 각 모듈 내부 `.cs` 파일 — using 문 갱신

| 프로젝트 | 수정할 파일 수 | 변경 내용 |
|---------|:------------:|----------|
| `Modules.SongBook` | ~10개 | `using MooldangBot.Contracts.SongBook.Interfaces` → 내부 Abstractions |
| `Modules.Roulette` | ~6개 | `using MooldangBot.Contracts.Roulette.Interfaces` → 내부 Abstractions |
| `Modules.Point` | ~6개 | `using MooldangBot.Contracts.Point.Interfaces` → 내부 Abstractions |
| `Modules.Commands` | ~8개 | `using MooldangBot.Contracts.Commands.Interfaces` → 내부 Abstractions |

---

## Phase 2: AppDbContext `OnModelCreating` 490줄 → 모듈별 `IEntityTypeConfiguration<T>` 분리

### 개요

현재 `AppDbContext.OnModelCreating`에 **490줄의 엔티티 구성 코드**가 밀집되어 있습니다. 이를 도메인 영역별 `IEntityTypeConfiguration<T>` 클래스로 분산합니다.

### 분리 전략

엔티티를 도메인 영역별로 7개의 구성 파일로 분리합니다:

| 구성 파일명 | 대상 엔티티 | 예상 줄 수 |
|-----------|-----------|:---------:|
| `CoreEntityConfigurations.cs` | StreamerProfile, GlobalViewer, StreamerManager, BroadcastSession, BroadcastHistoryLog, StreamerPreference | ~90줄 |
| `SongBookEntityConfigurations.cs` | SongQueue, SongBook, SonglistSession, StreamerOmakaseItem, Master_SongLibrary, Streamer_SongLibrary, Master_SongStaging | ~80줄 |
| `RouletteEntityConfigurations.cs` | Roulette, RouletteItem, RouletteLog, RouletteSpin | ~70줄 |
| `PointEntityConfigurations.cs` | ViewerRelation, ViewerPoint, ViewerDonation, ViewerDonationHistory, PointTransactionHistory, PointDailySummary | ~40줄 |
| `CommandEntityConfigurations.cs` | UnifiedCommand, CommandExecutionLog, CommandExecutionSagaState | ~50줄 |
| `OverlayEntityConfigurations.cs` | AvatarSetting, OverlayPreset, SharedComponent, PeriodicMessage, ChzzkCategory, ChzzkCategoryAlias | ~50줄 |
| `PhilosophyEntityConfigurations.cs` | IamfScenario, IamfGenosRegistry, IamfParhosCycle, IamfVibrationLog, IamfStreamerSetting, StreamerKnowledge | ~50줄 |
| `LedgerEntityConfigurations.cs` | RouletteStatsAggregated, ChatInteractionLog | ~20줄 |

### 변경 상세

#### [NEW] `Infrastructure/Persistence/Configurations/` 디렉토리

- 위 8개의 Configuration 파일 생성
- 각 파일에서 `IEntityTypeConfiguration<T>` 구현

#### [MODIFY] `Infrastructure/Persistence/AppDbContext.cs`

- `OnModelCreating` 내에서 개별 엔티티 구성 코드 제거
- 대신 `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);` 한 줄로 대체
- **유지할 코드** (전역 로직이므로 `OnModelCreating`에 남겨둠):
  1. CharSet/Collation 전역 설정 (L94~99)
  2. `EncryptedValueConverter` 인스턴스 생성 (L101~102)
  3. `ISoftDeletable` 전역 쿼리 필터 자동화 (L104~117)
  4. 암호화 필드 설정 (StreamerProfile 토큰, GlobalViewer UID) — Configuration 파일에서 `IServiceProvider` 접근 불가 시 남겨둠

> [!IMPORTANT]
> **암호화 컨버터 의존성 주의**: `EncryptedValueConverter`는 `IDataProtector`를 생성자에서 받아 `OnModelCreating`에서 사용합니다.
> `IEntityTypeConfiguration<T>`는 DI 컨테이너에서 직접 인스턴스화되지 않으므로, 암호화 컨버터가 필요한 엔티티(StreamerProfile, GlobalViewer)의 구성은 `OnModelCreating` 내부에 잔류하거나, 컨버터를 Configuration 생성자로 전달하는 패턴을 사용해야 합니다.
>
> **권장**: `modelBuilder.ApplyConfigurationsFromAssembly`를 호출한 뒤, 암호화 관련 구성만 `OnModelCreating`에서 후처리

### 리팩터링 후 `OnModelCreating` 예상 모습

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // [전역] CharSet/Collation 설정
    if (Database.IsMySql())
    {
        modelBuilder.HasCharSet("utf8mb4").UseCollation("utf8mb4_unicode_ci");
    }

    // [전역] ISoftDeletable 쿼리 필터 자동화
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
        {
            // ... 기존 필터 로직 ...
        }
    }

    // [모듈별] 엔티티 구성 일괄 적용
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    // [후처리] 암호화 컨버터 적용 (DI 의존성 필요)
    var converter = new EncryptedValueConverter(_protector);
    modelBuilder.Entity<StreamerProfile>(entity => {
        entity.Property(e => e.ChzzkAccessToken).HasConversion<string?>(converter);
        entity.Property(e => e.ChzzkRefreshToken).HasConversion<string?>(converter);
    });
    modelBuilder.Entity<GlobalViewer>(entity => {
        if (Database.IsMySql())
        {
            entity.Property(e => e.ViewerUid).HasColumnType("longtext").HasConversion(converter);
        }
        else
        {
            entity.Property(e => e.ViewerUid).HasConversion(converter);
        }
    });
}
```

> **결과**: `OnModelCreating` 490줄 → ~40줄로 감소

---

## Phase 3: 워커 등록 통합 — `WorkerRegistry.cs`

### 개요

현재 15개의 `BackgroundService`가 3개 이상의 DI 등록 지점에 분산되어 있어, 어떤 워커가 어디서 어떤 주기로 실행되는지 파악하기 어렵습니다.

| 현재 등록 위치 | 워커 수 |
|-------------|:-------:|
| `Application/DependencyInjection.cs` → `AddBotEngineServices()` | 10개 |
| `Application/DependencyInjection.cs` → `AddWebApiWorkers()` | 1개 |
| `Infrastructure/DependencyInjection.cs` | 3개 (ChatLogBatchWorker, PointWriteBackWorker, StagingCleanupWorker) |
| `Modules.Roulette/DependencyInjection.cs` | 1개 (RouletteResultWorker) |
| `ChzzkAPI/Program.cs` | 1개 (GatewayWorker) |

### 변경 상세

#### [NEW] `Application/Workers/WorkerRegistry.cs`

한눈에 모든 워커를 관리할 수 있는 선언적 등록 파일:

```csharp
namespace MooldangBot.Application.Workers;

/// <summary>
/// [파로스의 관제탑]: 전체 시스템의 BackgroundService 등록을 한 곳에서 관리합니다.
/// 신규 워커를 추가하거나 기존 워커의 주기를 변경할 때 이 파일만 수정하면 됩니다.
/// </summary>
public static class WorkerRegistry
{
    /// <summary>
    /// [봇 엔진 전용] 채팅 처리, 배치 쓰기, 토큰 갱신 등 봇 프로세스 워커 일괄 등록
    /// </summary>
    public static IServiceCollection AddBotEngineWorkers(this IServiceCollection services)
    {
        // ──────────────────────────────────────────────────
        // 상시 실행 워커
        // ──────────────────────────────────────────────────
        services.AddSingleton<ChzzkBackgroundService>();
        services.AddHostedService(sp => sp.GetRequiredService<ChzzkBackgroundService>());

        // ──────────────────────────────────────────────────
        // 고빈도 배치 워커 (1~10초 주기)
        // ──────────────────────────────────────────────────
        services.AddHostedService<PointBatchWorker>();             // 1초
        services.AddHostedService<ChatLogBatchWorker>();           // 1초
        services.AddHostedService<LogBulkBufferWorker>();          // 10초
        services.AddHostedService<PointWriteBackWorker>();         // 캐시 Dirty 시

        // ──────────────────────────────────────────────────
        // 중빈도 워커 (분 단위)
        // ──────────────────────────────────────────────────
        services.AddHostedService<TokenRenewalBackgroundService>(); // 5분
        services.AddHostedService<SystemWatchdogService>();         // 1분
        services.AddHostedService<PeriodicMessageWorker>();         // 설정값

        // ──────────────────────────────────────────────────
        // 저빈도 워커 (시간~일 단위)
        // ──────────────────────────────────────────────────
        services.AddHostedService<CategoriesyncBackgroundService>(); // 6시간
        services.AddHostedService<RouletteLogCleanupService>();      // 24시간
        services.AddHostedService<CelestialLedgerWorker>();          // 6시간
        services.AddHostedService<WeeklyStatsReporter>();            // 7일
        services.AddHostedService<StagingCleanupWorker>();           // 24시간

        // ──────────────────────────────────────────────────
        // 모듈 워커
        // ──────────────────────────────────────────────────
        services.AddHostedService<RouletteResultWorker>();           // 이벤트 기반

        return services;
    }

    /// <summary>
    /// [API 전용] 웹 API 서버에서만 실행되는 워커 일괄 등록
    /// </summary>
    public static IServiceCollection AddApiWorkers(this IServiceCollection services)
    {
        services.AddHostedService<ZeroingWorker>();                 // 자정 초기화
        return services;
    }
}
```

#### [MODIFY] `Application/DependencyInjection.cs`

- `AddBotEngineServices()` 메서드에서 워커 등록 코드 제거 → `AddBotEngineWorkers()` 호출로 대체
- `AddWebApiWorkers()` 메서드에서 워커 등록 코드 제거 → `AddApiWorkers()` 호출로 대체

#### [MODIFY] `Infrastructure/DependencyInjection.cs`

- `ChatLogBatchWorker`, `PointWriteBackWorker`, `StagingCleanupWorker`의 `AddHostedService<>()` 호출 제거
- 해당 워커의 구동 의존성 서비스(예: `IChatLogBufferService`)는 Infrastructure DI에 유지

#### [MODIFY] `Modules.Roulette/DependencyInjection.cs`

- `RouletteResultWorker`의 `AddHostedService<>()` 호출 제거
- 나머지 서비스 등록은 그대로 유지

> [!IMPORTANT]
> `WorkerRegistry.cs`가 다른 프로젝트의 워커 타입을 참조하려면 **Application → Infrastructure** 참조가 필요합니다.
> 현재 `Infrastructure → Application` 방향이므로, 역방향 참조는 순환을 유발합니다.
>
> **해결 방안**:
> 1. `WorkerRegistry.cs`를 **`Infrastructure` 프로젝트** 내부(`Infrastructure/Workers/WorkerRegistry.cs`)에 배치 — Infrastructure는 이미 Application을 참조하므로 양쪽 워커 모두 접근 가능
> 2. 또는 `Api` 프로젝트(진입점)에 `WorkerRegistry.cs`를 배치 — 모든 프로젝트를 참조하는 최상위 계층
>
> **권장**: `Infrastructure` 프로젝트에 배치 (Infrastructure는 Application + Modules 모두 참조 가능)

---

## 영향도 요약

| Phase | 변경 파일 수 | 위험도 | 회귀 범위 |
|:-----:|:-----------:|:-----:|----------|
| **Phase 1** | ~40개 | 🟡 중 | using 문 변경 → 컴파일 타임에 즉시 검증 |
| **Phase 2** | ~10개 | 🟢 낮음 | ModelBuilder 구성 → 기존 테스트 + 마이그레이션 스냅샷으로 검증 |
| **Phase 3** | ~5개 | 🟢 낮음 | DI 등록 이동 → 앱 기동 시 즉시 검증 |

---

## Open Questions

> [!IMPORTANT]
> **Q1. `IPointDbContext`의 Infrastructure 의존성 처리 방식**
> - **(A)** `PointWriteBackWorker`를 `IAppDbContext` 사용하도록 변경 (인터페이스 변경 최소화)
> - **(B)** `Infrastructure.csproj`에 `Modules.Point` 참조 추가 (모듈 순수성 유지)
> - 어떤 방식이 좋을까요?

> [!IMPORTANT]
> **Q2. `WorkerRegistry.cs` 배치 위치**
> - **(A)** `Infrastructure/Workers/WorkerRegistry.cs` — Infrastructure가 Application과 Modules 모두 참조 가능
> - **(B)** `Api/WorkerRegistry.cs` — 진입점에서 모든 것을 조합
> - 권장은 (A)입니다. 동의하시나요?

> [!IMPORTANT]
> **Q3. 중복 ToTable() 호출 정리**
> 현재 `OnModelCreating`에 동일 엔티티에 대한 `ToTable()` 호출이 **중복**으로 존재합니다 (예: `SongBook` → L378, L460). 
> Configuration 분리 시 이 중복을 정리해도 될까요?

---

## Verification Plan

### Automated Tests

```powershell
# 1. 전체 솔루션 빌드 — 컴파일 에러 검출
dotnet build MooldangAPI.sln

# 2. 기존 테스트 실행 — 회귀 없음 검증
dotnet test MooldangBot.Tests/MooldangBot.Tests.csproj

# 3. EF Core 마이그레이션 스냅샷 비교 — 모델 무결성 검증
dotnet ef dbcontext optimize --project MooldangBot.Infrastructure --startup-project MooldangBot.Api
```

### Manual Verification
- Docker 환경에서 앱 기동 후 모든 워커가 정상 시작하는지 로그 확인
- 주요 API 엔드포인트(룰렛, 곡 신청, 포인트) 기능 동작 확인
