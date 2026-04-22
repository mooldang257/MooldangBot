# [P1] API 아키텍처 고도화 및 일관성 강화 (성능 및 정규화)

선장님, P0 단계의 안정화가 확인되었으므로 이제 시스템의 성능을 끌어올리고 구조적 부채를 해결하는 **P1 단계**를 제안합니다. 이 단계에서는 API의 일관성을 확보하고 DB 부하를 줄이기 위한 캐싱 및 페이징 고도화에 집중합니다.

## User Review Required

> [!IMPORTANT]
> **1. API 버전 관리 제거 (`/api/v1/...` → `/api/...`)**
> 현재 프로젝트는 단일 버전을 유지하고 있으므로, 관리 복잡도를 줄이기 위해 `Asp.Versioning` 모듈을 제거하고 경로를 단순화할 계획입니다. 모든 API 엔드포인트는 루트 `/api/` 하위로 통합됩니다.
>
> **2. 데이터 로딩 방식의 전면 전환 (Cursor-based)**
> 모든 목록 조회 UI가 페이지 번호 방식에서 **'더보기(Load More)'** 또는 **'무한 스크롤'** 방식으로 전환됩니다. 이는 대규모 데이터셋에서의 성능 최적화와 데이터 정합성(중복 방지)을 위한 결정입니다. `ChatPoint` 등에서 사용 중인 `offset/limit` 방식을 `LastId` 기반의 커서 페이징으로 완전히 대체합니다.

## Proposed Changes

### 1. [Infrastructure] 공통 기반 고도화

#### [MODIFY] [Paging.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Domain/Common/Paging.cs)
- `PagedResponse<T>`를 `CursorPagedResponse<T>`로 정규화하여 프론트엔드에서 다음 데이터 존재 여부(`hasNext`)와 다음 커서(`nextCursor`)를 쉽게 식별할 수 있도록 필드 추가.
- `PagedRequest`에 정렬(Sort) 및 필터링 필드 추가.

#### [DELETE] [VersioningExtensions.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Extensions/VersioningExtensions.cs)
- `Asp.Versioning` 관련 설정을 제거하고 `AddMooldangVersioning()` 메서드에서 버전 관리 로직 삭제 (FluentValidation 설정은 유지).
- `Program.cs`에서 `AddMooldangVersioning()` 호출부 정리.

---

### 2. [Backend] 컨트롤러 정규화 및 캐싱 확대

#### [MODIFY] [ChatPointController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/ChatPoints/ChatPointController.cs)
- **페이징 정규화**: `GetViewers`, `GetDonations`의 `offset/limit`을 제거하고 `PagingExtensions`를 사용하여 커서 기반으로 전환.
- **DTO 명시**: 익명 객체 반환(`Result<object>`)을 제거하고 `Result<ViewerPointCursorResponse>` 등 명시적 DTO 모델 적용.

#### [MODIFY] [DashboardController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Dashboard/DashboardController.cs)
- **경로 정규화**: `api/dashboard/summary/{uid}` → `api/dashboard/{uid}/summary`로 변경하여 UID를 경로 상단으로 이동.
- **성능 최적화 (캐싱)**: `IMemoryCache`를 사용한 대시보드 요약 데이터 캐싱(TTL 30~60초)을 도입하여 반복적인 DB 집계 부하 차단.
- **성능 최적화 (Identity)**: `IIdentityCacheService`를 사용하여 스트리머 프로필 조회 시 DB 히트 차단.

#### [MODIFY] [BotConfigController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Config/BotConfigController.cs)
- **도메인 명확화**: `api/settings/bot` → `api/config/bot`으로 경로 변경.
- 불필요한 `POST` 메서드를 비즈니스 의미에 맞는 `PATCH`로 개선 (예: ToggleBotStatus).

---

### 3. [Frontend] API 통신 계층 일원화 및 동기화

#### [MODIFY] Admin / Studio 공통 API 클라이언트
- 신규 커서 페이징 인터페이스(`nextCursor`, `hasNext`)를 처리할 수 있도록 공통 데이터 헬퍼 함수 업데이트.
- 변경된 정규화 경로(`config/bot`, `dashboard/{uid}/summary` 등) 반영.

## Open Questions

- (모든 질문이 해결되었습니다. 선장님의 승인에 따라 작업을 시작합니다.)

## Verification Plan

### Automated Tests
- `dotnet build`: 도메인 모델 및 DTO 변경에 따른 컴파일 오류 점검.
- **API Unit Test**: 신규 커서 페이징 로직의 데이터 일관성 검증 (NextLastId가 정확히 반환되는지).

### Manual Verification
- **Admin/Studio 대시보드**: 캐시 적용 후 초기 진입 시 체감 속도 향상 확인.
- **명령어/시청자 목록**: 커서 기반 페이징이 프론트엔드 'Next' 요청 시 중복이나 누락 없이 데이터를 가져오는지 확인.
