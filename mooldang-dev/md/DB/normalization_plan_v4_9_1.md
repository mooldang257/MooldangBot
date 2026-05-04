# v4.9 Database Normalization (Philosophy² & Resilience Engine)

본 계획은 MooldangBot의 '철학적 지표(IAMF)'를 시스템 전역 관리 방식에서 개별 스트리머 독립 계층으로 분리하여 데이터 무결성과 확장성을 확보하는 것을 목표로 합니다.

## User Review Required

> [!IMPORTANT]
> **브레이킹 체인지**: `IamfParhosCycles`, `IamfGenosRegistry`, `IamfScenarios` 등 모든 철학 엔진 관련 엔티티에 `StreamerProfileId` (int)가 강제됩니다. 특히 `IamfParhosCycles`의 `CycleId`는 PK에서 일반 속성으로 변경됩니다.
> 
> **데이터 이관 전략 (CLI)**: SQL 스크립트 대신 `MooldangBot.Cli` 프로젝트를 실행하여 데이터를 이관합니다. 기존 전역 데이터는 관리자 계정(`StreamerProfileId = 1`)으로 자동 매핑됩니다.

## Proposed Changes

### 1. Domain Layer (엔티티 수정)

#### [MODIFY] [CoreStreamerProfiles.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Domain/Entities/CoreStreamerProfiles.cs)
- `DelYn` (N/Y), `MasterUseYn` (Y/N) 속성 추가.
- 논리적 삭제 및 마스터 차단 로직의 기반 마련.

#### [MODIFY] [IamfEntities.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Domain/Entities/Philosophy/IamfEntities.cs)
- `IamfParhosCycles`:
    - `int Id` [PK] 추가 (정규화).
    - `int CycleId`를 일반 속성으로 변경 (채널별 순번).
    - `int StreamerProfileId` [FK] 추가.
    - `ParhosId` 제거 (스트리머 종속으로 불필요).
- `IamfGenosRegistry`:
    - `int StreamerProfileId` [FK] 추가 (스트리머별 가상 AI 메타포 격리).
- `IamfScenarios`:
    - `int StreamerProfileId` [FK] 추가 (철학 엔진의 뼈대인 시나리오를 스트리머별로 완전히 분리).

---

### 2. Infrastructure Layer (영속성 구성)

#### [MODIFY] [AppDbContext.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/Persistence/AppDbContext.cs)
- `OnModelCreating` 구성:
    - `IamfParhosCycles`: `[StreamerProfileId, CycleId]` 복합 유니크 인덱스 설정.
    - `DeleteBehavior.Restrict` 설정: `CoreStreamerProfiles` 삭제(물리) 시 데이터 보존(소실 방지).
    - **전역 쿼리 필터 최적화**:
        - `CoreStreamerProfiles` 엔티티 본체에만 `DelYn == "N" && MasterUseYn == "Y"` 필터를 적용합니다.
        - **주의**: `IamfParhosCycles`, `IamfScenarios` 등 자식 엔티티에는 부모와의 암묵적 JOIN을 유발하는 필터를 적용하지 않습니다. 자식 엔티티 단독 조회 시 성능을 최우선으로 하며, 활성 상태 검증은 Application Layer(캐시 및 세션)에서 선행 처리합니다.

---

### 3. Application Layer (성능 및 로직 리팩토링)

#### [MODIFY] [ResonanceService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Services/Philosophy/ResonanceService.cs)
- **전략 A: 지연 로딩 (Lazy Loading) 도입**
    - `ConcurrentDictionary<int, ParhosState>`를 통한 스트리머별 독립 상태 관리.
    - 패킷 유입 시 메모리에 상태가 없을 경우, DB(`IamfParhosCycles`)에서 가장 최근의 `VibrationAtDeath`를 조회하여 복구(Hydration).
    - DB 기록이 없는 신규 스트리머의 경우에만 기본값(10.01Hz)으로 초기화.

#### [MODIFY] [BroadcastScribe.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Services/Philosophy/BroadcastScribe.cs)
- `_activeStats`, `_liveCheckCooldown`, `_recentChatActivity` 등의 Key 타입을 `string (chzzkUid)`에서 `int (StreamerProfileId)`로 전면 교체.

### 4. Migration & Maintenance (데이터 보존)

#### [MODIFY] [Program.cs (Cli)](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Cli/Program.cs)
- v4.9 데이터 이관 로직 추가:
    - 기존 전역 `IamfParhosCycles`, `IamfGenosRegistry`, `IamfScenarios` 데이터를 `StreamerProfileId = 1` 계정으로 일괄 이관하는 멱등성(Idempotent) 있는 태스크 구현.

---

### 5. Documentation (md/DB 폴더)

#### [NEW] [normalization_process_v4_9.md](file:///c:/webapi/MooldangAPI/MooldangBot/md/DB/normalization_process_v4_9.md)
- 단계별 작업 기록 및 이슈 추적.

#### [NEW] [normalization_result_v4_9.md](file:///c:/webapi/MooldangAPI/MooldangBot/md/DB/normalization_result_v4_9.md)
- 최종 정규화 결과 보고서.

## Open Questions

> [!NOTE]
> - `IamfScenarios` 분리 결정: 아키텍트의 피드백에 따라 v4.9 범위에 포함하여 완전히 분리합니다.
> - 데이터 이관 시점: `dotnet run --project MooldangBot.Cli/MooldangBot.Cli.csproj` 실행을 통해 마이그레이션과 데이터 필드가 동시에 보정되도록 구성합니다.

## Verification Plan

### Automated Tests (Logical)
- `dotnet build`: 전체 솔루션 컴파일 성공 여부 확인.
- Fluent API 설정 검증: `Unique Index` 및 `Restrict` 설정 코드 리뷰.

### Manual Verification (Logical Analysis)
- `CoreStreamerProfiles` 필터링 로직 검증: `DelYn`이 포함된 쿼리가 인덱스를 타는지 확인.
- `ConcurrentDictionary`를 통한 동시성 제어 로직 검증.
