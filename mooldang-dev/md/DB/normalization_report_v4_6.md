# 📘 v4.6 Configuration & Overlay 도메인 정문화 완료 보고서

## 1. 개요 및 목적 (Purpose)
본 작업은 'MooldangBot' 시스템의 **Configuration & Overlay** 도메인 테이블들을 제3정규형(3NF)으로 마이그레이션하여 데이터베이스의 무결성을 확보하고 멀티테넌트 격리 수준을 강화하는 것을 목적으로 합니다.

- **데이터 격리**: 스트리머별 설정(`SysAvatarSettings`, `SysOverlayPresets` 등)을 문자열 UID가 아닌 정수형 `StreamerProfileId` 외래 키로 관리하여 데이터 참조 속도를 높이고 무결성을 보장합니다.
- **성능 최적화**: 문자열 검색 기반의 조인을 정수형 인덱스 기반으로 전환하여 대규모 트래픽 상황에서의 병목 현상을 방지합니다.
- **안정성 강화**: 전역 쿼리 필터와 연쇄 삭제(Cascade) 정책을 통해 고아 데이터 발생을 차단합니다.

## 2. 설계 철학 (Philosophy)
> **"오시리스의 시선: 명확한 소유권과 유기적인 데이터 흐름"**

- **단일 소유권 원칙**: 모든 설정은 반드시 하나의 `CoreStreamerProfiles`에 귀속되어야 하며, 식별자는 변하지 않는 내부 ID를 사용합니다.
- **최대 안정성**: 정문화 과정에서 발생할 수 있는 부수적인 빌드 오류(Collateral Build Errors)를 즉각 탐지하고 해결하여 시스템 전체의 정적 안정성을 유지합니다.
- **클린 코드**: 불필요한 중복(Redundancy)을 제거하고 navigation property를 적극 활용하여 가독성 높은 비즈니스 로직을 구현합니다.

## 3. 작업 내용 (Tasks)

### 엔티티 리팩토링 (Domain Layer)
- `SysAvatarSettings`, `SysOverlayPresets`, `SysPeriodicMessages`, `SysSharedComponents` 엔티티에서 `ChzzkUid` 필드를 제거하고 `StreamerProfileId`를 도입했습니다.
- 가독성을 위해 `CoreStreamerProfiles` 가상 속성(Virtual Property) 및 외래 키 어노테이션을 추가했습니다.

### 인프라 매핑 (Infrastructure Layer)
- `AppDbContext`의 Fluent API를 통해 각 엔티티의 연쇄 삭제 정책 및 고유 인덱스를 선언했습니다.
- 스트리머 전용 글로벌 쿼리 필터를 업데이트하여 보안 격리를 강화했습니다.

### 컨트롤러 및 워커 수정 (Application & Presentation Layer)
- 관련 컨트롤러(`SysAvatarSettings`, `SysOverlayPresets` 등)의 모든 CRUD 로직을 정수형 ID 기반으로 리터럴 전환했습니다.
- 백그라운드에서 동작하는 `PeriodicMessageWorker` 및 `SystemWatchdogService`를 최신 정문화 구조에 맞춰 수정했습니다.

## 4. 작업 파일 리스트 (Modified Files)

### Domain
- [SysAvatarSettings.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Domain/Entities/SysAvatarSettings.cs)
- [SysOverlayPresets.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Domain/Entities/SysOverlayPresets.cs)
- [SysPeriodicMessages.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Domain/Entities/SysPeriodicMessages.cs)
- [SysSharedComponents.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Domain/Entities/SysSharedComponents.cs)

### Infrastructure
- [AppDbContext.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Infrastructure/Persistence/AppDbContext.cs)
- [NormalizeConfigV46.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Infrastructure/Migrations/20260401154556_NormalizeConfigV46.cs) (Migration)

### Application & Presentation
- [AvatarSettingsController.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Presentation/Features/Avatar/AvatarSettingsController.cs)
- [OverlayPresetController.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Presentation/Features/Overlay/OverlayPresetController.cs)
- [PeriodicMessageController.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Presentation/Features/Shared/PeriodicMessageController.cs)
- [SharedComponentController.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Presentation/Features/Shared/SharedComponentController.cs)
- [PeriodicMessageWorker.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Application/Workers/PeriodicMessageWorker.cs)
- [SystemWatchdogService.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Application/Workers/SystemWatchdogService.cs) (Collateral Fix)
- [SongBookController.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Presentation/Features/FuncSongBooks/SongBookController.cs) (Collateral Fix)
- [OmakaseEventHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot.Application/Features/FuncSongBooks/Handlers/OmakaseEventHandler.cs) (Collateral Fix)

## 5. 최종 검증 결과 (Verification)
- **정적 분석**: `dotnet build` 수행 시 경고 8개(정상 범위), 오류 0개로 빌드 성공.
- **스키마 적용**: `ef database update`를 통해 MariaDB에 `NormalizeConfigV46` (Drop & Create 전술) 적용 완료.
- **무결성**: 외래 키 제약 조건을 통한 부모-자식 관계 설정 및 삭제 동작 검증 완료.

---
**보고서 작성일**: 2026-04-02
**작성자**: 물멍 (Senior Full-Stack Partner)
