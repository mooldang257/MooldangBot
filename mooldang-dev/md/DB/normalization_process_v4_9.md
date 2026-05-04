# v4.9 정규화 작업 기록 (Normalization Process)

본 문서는 v4.9 (Philosophy² & Resilience Engine) 정문화 작업의 단계별 이행 과정을 기록합니다.

## 작업 단계 기록

### 단계 1: 도메인 기반 마련 (Domain Layer)
- `CoreStreamerProfiles` 엔티티 확장: `DelYn`, `MasterUseYn` 필드 추가로 멀티테넌트 관리 기반 구축.
- 철학 엔티티(`IamfParhosCycles`, `IamfGenosRegistry`, `IamfScenarios`)에 `StreamerProfileId` 외래키 도입 및 PK 구조 정규화 완료.

### 단계 2: 인프라 구성 및 제약 조건 설정 (Infrastructure Layer)
- `AppDbContext` Fluent API 설정:
    - `IamfParhosCycles` 복합 유니크 인덱스 `[StreamerProfileId, CycleId]` 적용.
    - `DeleteBehavior.Restrict` 설정을 통한 존재의 보존(데이터 소실 방지) 철학 구현.
    - 전역 쿼리 필터 최적화: `CoreStreamerProfiles` 본체에만 필터 적용하여 JOIN 부하 최소화.

### 단계 3: EF Core 마이그레이션 및 데이터 이관 SQL 주입
- `AddPhilosophyResilience_v4_9` 마이그레이션 생성.
- **커스텀 SQL 주입**:
    - 시스템 관리자(ID:1) 프로필 자동 생성.
    - 기존 전역 데이터를 관리자 계정으로 이관 (Idempotent UPDATE).
    - 외래키 제약 조건 충돌 방지를 위한 기본값 보정.

### 단계 4: 애플리케이션 서비스 리팩토링 (Application Layer)
- `ResonanceService`: `ConcurrentDictionary<int, ParhosState>` 및 `_uidToIdMap`을 통한 고성능 멀티테넌트 상태 관리 구현.
- `BroadcastScribe`: 모든 세션 키를 `int (StreamerProfileId)`로 전환하여 데이터 정합성 강화 및 메모리 효율 증대.

### 단계 5: 툴 및 유지보수 (Cli)
- `MooldangBot.Cli` 빌드 오류 해결: 정문화된 필드 이름(`ChannelName`) 반영.
- 데이터 보정 태스크 최적화.

## 발생 이슈 및 해결
- **이슈**: `CoreStreamerProfiles`에 `Nickname` 필드가 없어 CLI 프로젝트 빌드 실패.
- **해결**: 설계서에 따라 `ChannelName` 필드로 대체하여 초기화 로직 수정.
- **이슈**: 마이그레이션 생성 시 `StreamerProfileId` 기본값 0으로 인한 FK 제약 조건 위반 우려.
- **해결**: 마이그레이션 `Up` 메서드에 관리자 계정 생성 및 데이터 이관 SQL을 선행 주입하여 해결.
