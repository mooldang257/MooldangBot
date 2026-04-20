# [P1] API 아키텍처 고도화 및 일관성 강화

보고서에서 제안된 P1(단기) 우선순위 항목을 해결하기 위한 상세 구현 계획입니다. 이 작업은 P0 작업이 완료된 후 수행되는 것을 전제로 합니다.

## User Review Required

> [!IMPORTANT]
> **데이터 로딩 방식의 변화**
> 페이지네이션이 커서 기반으로 일원화됨에 따라 프론트엔드의 리스트 로딩 로직이 전면 수정되어야 합니다. 기존의 `Page 1, 2, 3...` 방식이 아닌 `더보기(NextCursor)` 방식으로 UI 대응이 필요할 수 있습니다.

## Proposed Changes

### 1. [Infrastructure] 공통 기반 고도화

#### [MODIFY] [PagingExtensions.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Domain/Common/Extensions/PagingExtensions.cs)
- `PagedResponse` 처리 로직을 `CursorPagedResponse`와 완전 통합
- `IQueryable` 확장 메서드를 통해 모든 목록 조회가 일관된 커서 인터페이스를 반환하도록 수정

#### [DELETE] [VersioningExtensions.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Extensions/VersioningExtensions.cs)
- `Asp.Versioning` 관련 설정을 제거하고 `Program.cs`에서 관련 파이프라인 정리
- 유일하게 사용되던 `v1` 경로를 기본 경로(`api/`)로 흡수

---

### 2. [Backend] 컨트롤러 및 라우트 정규화

#### [MODIFY] [ChatPointController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/ChatPoints/ChatPointController.cs)
- **페이지네이션**: `offset/limit` 패턴을 제거하고 `CursorPagedResponse<T>` 적용
- **타입 정규화**: `Result<object>` 대신 `Result<ChatPointSettingsDto>`, `Result<CursorPagedResponse<ViewerPointDto>>` 등으로 명시적 타입 반환
- **라우트**: `api/chatpoint` → `api/chat-point` (Kebab-case 준수)

#### [MODIFY] [DashboardController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Dashboard/DashboardController.cs)
- **라우트 정규화**: `api/dashboard/summary/{uid}` → `api/dashboard/{uid}/summary`
- **라우트 정규화**: `api/dashboard/activities/{uid}` → `api/dashboard/{uid}/activities`
- (이 도메인은 이미 DTO를 잘 사용 중이므로 경로 일관성 위주로 수정)

#### [MODIFY] [BotConfigController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Config/BotConfigController.cs)
- **라우트 정규화**: `api/settings/bot/status/{uid}` 등 `settings` 접두사를 `config`로 변경하여 도메인 경계 명확화
- **정규화 경로**: `api/config/bot/{uid}/status`

---

### 3. [Frontend] 데이터 통신 계층 일원화

#### [MODIFY] Admin / Studio API Client
- 두 프로젝트에 흩어진 `client.ts`의 SSR 처리 로직을 Studio의 `handleFetch` 패턴으로 통일
- 신규 커서 페이지네이션 인터페이스(`nextCursor`, `hasNext`)에 맞게 공통 데이터 헬퍼 수정

---

## Verification Plan

### Automated Tests
- `dotnet build`: API 버전 제거 및 DTO 전환에 따른 컴파일 오류 점검
- `curl` 또는 `Postman`을 이용한 라우트 경로 변경 이행 여부 전수 확인

### Manual Verification
- **시청자/도네이션 목록**: 무한 스크롤 또는 더보기 버튼이 정상적으로 커서 데이터를 가져오는지 확인
- **설정 페이지**: 익명 객체 반환이 제거되었음에도 프론트엔드에서 데이터 바인딩이 정상적인지 확인
- **로그인/권한**: `chzzkUid`가 경로 중앙으로 이동함에 따른 권한 검사 미들웨어(Aegis) 정상 동작 확인
