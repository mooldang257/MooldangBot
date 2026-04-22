# [P2] 통신 계층 고도화 및 품질 강화

보고서에서 제안된 P2(중기) 우선순위 항목을 해결하기 위한 상세 구현 계획입니다. P0, P1 작업이 완료된 후 시스템의 유지보수성과 안정성을 극대화하기 위한 단계입니다.

## User Review Required

- **SignalR 클라이언트 영향**: Strongly-typed Hub 적용 시 백엔드 메서드 시그니처가 변경될 수 있으나, 클라이언트(Overlay)에서의 호출 방식은 유지됩니다. 대신 백엔드 코드에서 오타로 인한 런타임 에러를 방지합니다.
- **AuthController 분리**: API 경로가 `api/auth/...`, `api/identity/...`, `api/proxy/...` 등으로 세분화되므로 프론트엔드에서 참조하는 컨트롤러 주소를 업데이트해야 합니다.

## Proposed Changes

### 1. [Infrastructure] SignalR 및 통신 안정성

#### [MODIFY] [IOverlayClient.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Hubs/IOverlayClient.cs) [NEW]
- 클라이언트로 전송되는 모든 이벤트 정의 (타입 안전성 확보)
  - `ReceiveOverlayState(string json)`
  - `ReceiveOverlayStyle(string json)`
  - `OnRouletteResult(object result)`
  - `OnSongQueueUpdate(object data)`

#### [MODIFY] [OverlayHub.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Hubs/OverlayHub.cs)
- `Hub` → `Hub<IOverlayClient>`로 상속 변경
- `await Clients.Group(...).SendAsync("EventName", ...)` 코드를 `await Clients.Group(...).EventName(...)` 형태로 리팩토링

#### [MODIFY] [Admin/src/hooks.server.ts](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Admin/src/hooks.server.ts)
- Studio 프로젝트와 동일하게 `handleFetch`를 도입하여 SSR 환경의 내부 API 호출 프록시 및 쿠키 전달 로직 통합

#### [MODIFY] [Admin/src/lib/api/client.ts](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Admin/src/lib/api/client.ts)
- `!browser` 판별 및 직접 URL 치환 로직 제거 (Infrastructure 계층으로 책임 전가)

---

### 2. [Backend] AuthController 책임 분리 (Decomposition)

#### [MODIFY] [AuthController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Auth/AuthController.cs)
- 로그인(`chzzk-login`), 콜백(`callback`), 로그아웃(`logout`) 핵심 로직만 유지

#### [NEW] [IdentityController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Auth/IdentityController.cs)
- `auth/me`, `resolve-slug`, `validate-access` 등 신원 확인 및 권한 검증 로직 이관

#### [NEW] [ProxyController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Auth/ProxyController.cs)
- `proxy/image` (치지직 이미지 우회) 로직 이관

#### [NEW] [AdminProfileController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Admin/AdminProfileController.cs)
- `admin/bot/streamers` 등 마스터 계정용 관리 로직 이관

---

### 3. [Shared] HTTP 메서드 및 의미론 정규화

#### [MODIFY] 컨트롤러 전수 조사
- 수정 작업에 `POST`를 사용 중인 곳을 `PUT` 또는 `PATCH`로 변경
  - 예: `RouletteController.UpdateRoulette`
- 대량 삭제 작업에 대응하는 `DELETE /bulk` 패턴 확립

---

## Verification Plan

### Automated Tests
- `dotnet build`: Typed Hub 적용에 따른 인터페이스 구현 누락 확인
- 유닛 테스트: 분리된 컨트롤러들이 기존 권한 미들웨어와 정상적으로 연동되는지 검증

### Manual Verification
- **오버레이 작동**: SignalR 이벤트가 정상적으로 수신되어 애니메이션이 구동되는지 확인
- **프론트엔드 SSR**: Admin 페이지를 새로고침(F5) 했을 때 서버 사이드에서 데이터를 정상적으로 가져오는지(`handleFetch` 작동 여부) 확인
- **이미지 프록시**: 대시보드와 오버레이에서 프로필 이미지가 정상 출력되는지 확인
