# [P0] API 통신 규약 정규화 및 안정성 강화

보고서에서 제안된 P0 우선순위 항목(안정성 위협 및 라우트 네이밍 불일치)을 해결하기 위한 상세 구현 계획입니다.

## User Review Required

> [!IMPORTANT]
> **파괴적 변경(Breaking Change) 주의**
> 이 작업은 API 엔드포인트 URL을 물리적으로 변경합니다. 백엔드 배포와 프론트엔드 배포가 동기화되지 않으면 UI의 일부 기능이 작동하지 않을 수 있습니다.

- **라우트 변경**: `api/SongRequest` → `api/song-request` 등
- **응답 구조 변경**: 엔티티 객체 반환 대신 하이드레이션된 DTO를 반환하여 순환 참조 에러를 원천 차단합니다.

## Proposed Changes

### 1. [Shared] Domain Dto 정의
순환 참조 방지 및 안전한 데이터 전달을 위해 엔티티 대신 사용할 전용 응답 DTO를 추가하거나 기존 DTO를 확장합니다.

#### [MODIFY] [RouletteResponseDto.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Domain/DTOs/RouletteDtos.cs) [NEW]
- `RouletteItem` 엔티티를 포함하되, 역참조(`I.Roulette`)가 없는 순수 DTO 정의

#### [MODIFY] [SongQueueDtos.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Domain/DTOs/SongQueueDtos.cs) [NEW]
- `SongQueue` 엔티티 대신 사용할 `SongQueueResponseDto` 정의

---

### 2. [Backend] 컨트롤러 리팩토링

#### [MODIFY] [RouletteController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Roulette/RouletteController.cs)
- `CreateRoulette`, `UpdateRoulette`의 반환 타입을 `Result<RouletteResponseDto>`로 변경
- 엔티티의 수동 `null` 처리 로직(`foreach ... = null`) 제거
- `UpdateRoulette` HTTP 메서드를 `POST` → `PUT`으로 변경

#### [MODIFY] [SongController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/SongQueue/SongController.cs)
- `AddSong`, `UpdateSongDetails`의 반환 타입을 `Result<SongQueueResponseDto>`로 변경
- `AddSong` 경로: `/api/song/add/{chzzkUid}` → `POST /api/song/{chzzkUid}`
- `DeleteSongs` 경로: `/api/song/delete/{chzzkUid}` → `DELETE /api/song/{chzzkUid}`

#### [MODIFY] [SongRequestController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/SongRequest/SongRequestController.cs)
- `[Route("api/SongRequest")]` → `[Route("api/song-request")]`

#### [MODIFY] [SharedComponentController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Shared/SharedComponentController.cs)
- `[Route("api/SharedComponent")]` → `[Route("api/shared-component")]`

#### [MODIFY] [PeriodicMessageController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/PeriodicMessage/PeriodicMessageController.cs)
- `[Route("api/PeriodicMessage")]` → `[Route("api/periodic-message")]`

#### [MODIFY] [OverlayPresetController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Overlay/OverlayPresetController.cs)
- `[Route("api/OverlayPreset")]` → `[Route("api/overlay-preset")]`

#### [MODIFY] [CommandsController.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Controllers/Commands/CommandsController.cs)
- `/api/commands/unified/save/{chzzkUid}` → `POST /api/commands/unified/{chzzkUid}`
- `/api/commands/unified/delete/{chzzkUid}/{id}` → `DELETE /api/commands/unified/{chzzkUid}/{id}`

---

### 3. [Frontend] API 호출 경로 업데이트

#### [MODIFY] Admin 및 Studio 전수 조사
- `src/` 폴더 내 변경된 API 경로를 사용하는 모든 `.svelte`, `.ts` 파일 수정
- 특히 `hooks.server.ts`의 정규화/변환 로직 내 경로 상수 확인

---

## Open Questions

- **동사 제거 시 충돌 여부**: 현재 `DELETE /api/song/{chzzkUid}`와 같이 리소스를 식별할 ID 없이 `body`로 전달하는 패턴들이 있습니다. 이를 `DELETE /api/song/{chzzkUid}/bulk`와 같이 명시할지, 아니면 현재처럼 리바인딩만 할지 결정이 필요합니다.
  - *제안*: 일관성을 위해 `body`를 사용하는 대량 작업은 `/bulk` 접미사를 붙여 의미를 명확히 하는 것이 좋습니다.

## Verification Plan

### Automated Tests
- `dotnet build`: 컴파일 타임의 타입 미스매치 확인
- `grep`을 통한 잔여 PascalCase 라우트 및 구형 경로 전수 검사

### Manual Verification
- **Admin 대시보드**: 신규 곡 신청, 룰렛 수정, 명령어 저장 기능이 정상 작동하는지 확인
- **Studio 설정**: 오버레이 프리셋 저장 및 불러오기 확인
- **네트워크 탭**: 크롬 개발자 도구의 Network 탭을 통해 모든 요청이 신규 정의된 kebab-case 및 RESTful 메서드로 가는지 확인
