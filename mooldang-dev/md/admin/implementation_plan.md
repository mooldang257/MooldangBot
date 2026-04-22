# 어드민 스트리머 관리 및 권한 제어 고도화 계획

## 1. 개요
봇 및 마스터 계정이 시스템에 등록된 모든 스트리머를 관리할 수 있도록 보안 정책을 개편하고, `admin.html`에서 전체 스트리머 목록 확인 및 대시보드(main.html) 접속 기능을 구현합니다.

## 2. 제안된 변경 사항

### 2.1 보안 및 권한 설정
- **[MODIFY] AuthController.cs**: 봇 계정(`445df9c4...`)에 `master` 역할을 부여하여 어드민 권한을 통합합니다.
- **[MODIFY] ChannelManagerAuthorizationHandler.cs**: 
  - `master` 역할(마스터/봇)은 모든 채널에 대해 `context.Succeed()` 처리 (프리패스).
  - **라우트 데이터 추출**: `IHttpContextAccessor` 또는 `context.Resource` 캐스팅을 통해 URL의 `{chzzkUid}`를 정확히 추출하여 권한 대조를 수행합니다. (⚠️ 중요: 이 값이 누락되면 일반 스트리머의 접근이 403으로 차단될 수 있음)

### 2.2 백엔드 API 구현
- **[MODIFY] AdminBotController.cs**: 
  - `GET /api/admin/bot/streamers` 엔드포인트 추가.
  - **최적화**: 홈 서버 메모리 효율을 위해 반드시 `.AsNoTracking()`을 적용하여 전체 스트리머 정보를 조회합니다.
  - 컨트롤러 클래스 상단에 `[Authorize(Roles = "master")]`를 명시하여 보안을 강화합니다.
- **[MODIFY] 주요 컨트롤러 리팩토링**:
  - `RouletteController`, `CommandsController`, `SongController`, `ChatPointController` 등.
  - `[Authorize(Policy = "ChannelManager")]`를 적용하고, 엔드포인트가 `chzzkUid`를 명시적으로 받도록 수정합니다. (예: `GET /api/roulette/{chzzkUid}`)
  - 데이터 조회 시 `.IgnoreQueryFilters()`를 사용하여 전역 필터를 우회합니다.

### 2.3 프론트엔드 UI 구현
- **[MODIFY] admin.html**: 
  - `등록된 스트리머 관리` 카드 섹션을 추가합니다.
  - 스트리머별 `관리하기` 버튼을 통해 `/main.html?uid={uid}`로 연결합니다.

## 3. 핵심 구현 가이드 (기술 요약)
- **핸들러 내 라우트 추출**:
  ```csharp
  var httpContext = context.Resource as HttpContext;
  var targetChzzkUid = httpContext?.Request.RouteValues["chzzkUid"]?.ToString();
  ```
- **읽기 전용 쿼리 최적화**:
  ```csharp
  var streamers = await _context.StreamerProfiles.AsNoTracking().IgnoreQueryFilters().ToListAsync();
  ```

## 4. 검증 계획
1. **마스터 권한 검증**: 마스터 계정으로 로그인 후 `admin.html`을 통해 다른 스트리머의 대시보드(`main.html?uid=...`)에 접속했을 때 모든 설정값이 정상 노출되는지 확인.
2. **권한 고립 검증**: 일반 스트리머 계정이 URL 파라미터의 `uid`만 바꿔서 타 채널 API 호출 시 `403 Forbidden` 발생 확인.
3. **봇 계정 검증**: 봇 계정의 `admin.html` 접근 및 스트리머 관리 기능 정상 동작 확인.