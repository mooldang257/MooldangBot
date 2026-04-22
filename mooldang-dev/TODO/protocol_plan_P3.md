# [P3] 아키텍처 자동화 및 보안 강화

보고서에서 제안된 P3(장기) 우선순위 항목을 해결하기 위한 상세 구현 계획입니다. 프로젝트가 안정화된 후 개발 생산성을 극대화하고 보안 모델을 선언적으로 관리하기 위한 단계입니다.

## User Review Required

- **코드 생성기(NSwag/TypeScript) 도입**: 백엔드 모델 변경 시 프론트엔드 코드가 자동으로 동기화됩니다. 이를 위해 별도의 빌드 스크립트나 도구 설치가 필요할 수 있습니다.
- **권한 체계의 중앙 집중화**: 각 컨트롤러에 흩어져 있던 `chzzkUid` 검증 로직이 미들웨어로 통합되므로, 라우팅 규칙을 엄격히 준수해야 합니다.

## Proposed Changes

### 1. [Infrastructure] 개발 생산성 및 타입 안전성 자동화

#### [MODIFY] [Program.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Api/Program.cs)
- `Swashbuckle.AspNetCore` 설치 및 Swagger UI 설정
- XML 주석을 활성화하여 API 문서에 컨트롤러 설명 포함

#### [NEW] [nswag.json](file:///c:/webapi/MooldangAPI/MooldangBot/nswag.json)
- NSwag CLI를 통해 백엔드 C# DTO와 컨트롤러 정보를 기반으로 SvelteKit용 TypeScript Fetch 클라이언트를 자동 생성하도록 구성

#### [NEW] [SignalREvents.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Hubs/SignalREvents.cs)
- SignalR에서 사용되는 모든 이벤트명(예: `ReceiveOverlayState`)을 상수로 정의하여 백엔드 전역에서 공유

---

### 2. [Security] Aegis Shield (권한 검사 미들웨어)

#### [NEW] [StreamerAuthorizationHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Security/StreamerAuthorizationHandler.cs)
- 라우트 데이터의 `{chzzkUid}`를 자동으로 추출하여 현재 사용자의 클레임(`StreamerId` 또는 `AllowedChannelId`)과 비교하는 Authorization Requirement 구현

#### [MODIFY] [SecurityExtensions.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Extensions/SecurityExtensions.cs)
- `chzzk-access`와 같은 이름의 정책을 정의하여, 컨트롤러에서 `[Authorize("chzzk-access")]` 한 줄로 모든 권한 검사가 끝나도록 설정

---

### 3. [Backend] 컨트롤러 로직 슬리밍 (Slimming)

#### [MODIFY] [SongController.cs], [RouletteController.cs] 등
- 컨트롤러 내부에서 반복적으로 수행하던 `chzzkUid` 일치 여부 확인 코드 제거 (Aegis Shield 미들웨어로 책임 이관)

---

## Verification Plan

### Automated Tests
- `nswag run`: TypeScript 코드 생성 성공 여부 확인
- 권한 테스트: 다른 스트리머의 UID를 경로에 넣어 API를 호출했을 때 403 Forbidden이 정상 반환되는지 확인

### Manual Verification
- **/swagger**: Swagger UI 페이지가 정상 출력되고 모든 API 엔드포인트가 노출되는지 확인
- **프론트엔드 타입 체크**: 생성된 TypeScript 파일을 프론트엔드에서 import 했을 때 IDE에서 타입 힌트가 정확히 나오는지 확인
