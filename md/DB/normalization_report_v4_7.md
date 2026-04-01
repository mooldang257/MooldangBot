# 데이터베이스 정규화 완료 보고서 (v4.7)

## 1. 목적 (Objective)
본 작업의 목적은 `Governance & Administration (권한 및 운영 관리)` 도메인의 핵심 테이블인 `StreamerManager`와 `StreamerOmakaseItem`을 제3정규형(3NF)으로 마이그레이션하여 데이터 무결성을 확보하고 조인 성능을 최적화하는 것입니다. 기존의 문자열 기반 UID 참조 체계를 정수형 외래 키(Foreign Key) 체계로 전환하여 멀티테넌트 환경에서의 데이터 격리 및 운영 효율성을 극대화합니다.

## 2. 작업 내용 (Work Content)
- **엔티티 리팩토링 (Domain Layer)**: 
    - `StreamerManager`: `StreamerChzzkUid` 및 `ManagerChzzkUid`를 제거하고 `StreamerProfileId`와 `GlobalViewerId`로 선언. 상호 중복 등록 방지를 위한 복합 유니크 인덱스 적용.
    - `StreamerOmakaseItem`: `ChzzkUid`를 `StreamerProfileId`로 교체.
- **데이터 접근 로직 최적화 (Infrastructure Layer)**:
    - `AppDbContext`의 Fluent API를 활용하여 `StreamerProfile` 및 `GlobalViewer`와의 관계(Navigation Property) 및 연쇄 삭제(Cascade Delete)를 명시적으로 정의.
- **빌드 오류 수정 (Cross-Layer)**:
    - `UnifiedCommandService`, `SongBookController`, `AuthController` 등 시스템 전반에서 기존 문자열 UID를 참조하던 비즈니스 로직을 정수형 ID 및 내비게이션 프로퍼티 참조 방식으로 전면 수정.
- **데이터 무손실 마이그레이션 (Persistence)**:
    - `NormalizeGovernanceV47` 마이그레이션 파일에 직접 SQL을 주입하여 기존 데이터를 유실 없이 정규화된 테이블로 이관. (SHA256 해싱을 통한 매니저 계정의 고유 식별자 복구 로직 포함)

## 3. 작업 파일 (Modified Files)
- **Domain**: 
    - `MooldangBot.Domain/Entities/StreamerManager.cs`
    - `MooldangBot.Domain/Entities/StreamerOmakaseItem.cs`
- **Infrastructure**:
    - `MooldangBot.Infrastructure/Persistence/AppDbContext.cs`
    - `MooldangBot.Infrastructure/Migrations/20260401210755_NormalizeGovernanceV47.cs`
- **Application**:
    - `MooldangBot.Application/Features/Commands/General/UnifiedCommandService.cs`
    - `MooldangBot.Application/Features/SongBook/Handlers/OmakaseEventHandler.cs`
- **Presentation**:
    - `MooldangBot.Presentation/Features/Auth/AuthController.cs`
    - `MooldangBot.Presentation/Features/SongBook/SongBookController.cs`
    - `MooldangBot.Presentation/Features/SongBook/SonglistSettingsController.cs`
    - `MooldangBot.Presentation/Features/SongQueue/SongController.cs`

## 4. 철학 (Philosophy)
이번 정규화 작업에는 MooldangBot 시스템의 핵심 철학인 **[위계의 질서]**와 **[존재의 격리]**가 반영되었습니다.

1. **[위계의 질서 - RBAC Integrity]**: 매니저 권한(`StreamerManager`)은 단순히 문자열 아이디의 일치가 아닌, `GlobalViewer`라는 추상화된 존재와 `StreamerProfile`이라는 주권자 사이의 명확한 관계 정립을 통해 관리되어야 합니다. 이를 통해 권한 부여의 정당성과 관리의 명확성을 확보합니다.
2. **[존재의 격리 - Multi-tenancy]**: 모든 운영 데이터(`OmakaseItem` 등)는 스트리머의 고유 식별자(`StreamerProfileId`)에 종속되어야 하며, 이는 데이터베이스 계층에서 외래 키와 인덱스를 통해 물리적으로 보장됩니다.
3. **[그림자의 기록 - Global Identification]**: 매니저와 시청자의 원본 UID를 직접 다루지 않고 `Sha256Hasher`와 `GlobalViewer`를 거쳐 해시화된 값으로 처리함으로써, 보안성과 개인정보 보호라는 현대 백엔드 설계의 필수 덕목을 실천합니다.

---
**보고자**: 시니어 풀스택 파트너 '물멍'
**날짜**: 2026-04-02
**상태**: 완료 (Build Succeeded, Migration Applied)
