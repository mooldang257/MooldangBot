# MooldangBot 명령어 시스템 정밀 분석 보고서

본 보고서는 MooldangBot의 핵심 기능인 명령어 처리 시스템의 아키텍처, 데이터 흐름, 그리고 세부 구현 사항을 분석한 결과입니다.

---

## 1. 명령어 처리 아키텍처 개요

MooldangBot은 **이벤트 드리븐 아키텍처(EDA)**를 기반으로 명령어를 처리합니다. 치지직 웹소켓을 통해 수신된 모든 메시지는 `MediatR` 이벤트를 통해 여러 핸들러로 병렬 전파됩니다.

### 🔄 전체 데이터 흐름
1. **[Infrastructure]** `ChzzkChatClient`가 웹소켓 패킷 수신 및 파싱.
2. **[Domain]** `ChatMessageReceivedEvent` 발행 (채팅 내용, 후원 금액, 발신자 정보 포함).
3. **[Application]** 다수의 `INotificationHandler<ChatMessageReceivedEvent>`가 이벤트를 수신하여 각자의 로직 수행.
4. **[Infrastructure]** `ChzzkBotService` 또는 `OverlayHub`를 통해 채팅 회신 및 오버레이 갱신.

---

## 2. 명령어 유형별 동작 상세

### 2.1 커스텀 명령어 시스템
- **관련 파일**: `CustomCommandEventHandler`, `CommandCacheService`, `CommandsController`
- **동작 원리**: 
    - 사용자가 `commands.html`에서 등록한 명령어는 `streamercommands` 테이블에 저장됩니다.
    - `CommandCacheService`가 스트리머별 명령어를 메모리에 캐싱하여 성능을 최적화합니다.
- **액션 타입**:
    - **Reply**: 채팅창에 설정된 텍스트로 즉시 회신 (`{닉네임}` 변수 지원).
    - **Notice**: 오버레이 상단 공지사항 업데이트.
    - **SonglistToggle**: 송리스트 세션 활성/비활성 전환 및 `{송리스트상태}` 치환 응답.

### 2.2 곡 신청 및 오마카세 시스템
- **관련 파일**: `OmakaseEventHandler`, `SongBookController`
- **동작 원리**:
    - `!신청 [제목]` 또는 `!물마카세` 명령어를 감지합니다.
    - **후원 연동**: 설정된 금액 이상의 후원(JSON 패킷 내 `donationAmount`)이 들어올 때만 작동하도록 설계되었습니다.
    - **세션 체크**: `SonglistSessions` 테이블을 조회하여 현재 스트리머가 신청을 받고 있는 상태인지 검증합니다.

### 2.3 룰렛 시스템
- **관련 파일**: `RouletteEventHandler`, `RouletteService`
- **동작 원리**:
    - **치즈 룰렛**: 후원 금액을 `CostPerSpin`으로 나눈 몫만큼 자동 다회차 실행.
    - **포인트 룰렛**: 시청자의 보유 포인트를 `ViewerProfiles`에서 차감 후 실행.
    - **동시성 제어**: `SemaphoreSlim`과 DB 트랜잭션을 사용하여 포인트 차감 및 당첨 기록의 무결성을 보장합니다.

### 2.4 채널 관리 명령어
- **관련 파일**: `ChannelSettingEventHandler`, `ChzzkApiClient`
- **동작 원리**:
    - `!방제 [제목]`, `!카테고리 [이름]` 명령어를 처리합니다.
    - 치지직 Open API의 `PATCH` 요청을 통해 실제 방송 설정을 실시간으로 변경합니다.

---

## 3. 핵심 기술 요소 및 제약 사항

### 🛡️ 권한 관리 (RBAC)
각 핸들러는 `notification.UserRole`을 확인하여 권한을 제어합니다.
- `streamer`: 소유자 전역 권한.
- `manager`: 채널 관리자 권한 (토글 명령어 등 수행 가능).
- `all`: 일반 시청자 포함 전체.

### 🧠 메모리 캐싱 전략
- `CommandCacheService`를 통해 `ConcurrentDictionary`에 명령어 정보를 상주시켜 매 채팅마다 발생하는 DB I/O를 방지합니다.
- 설정 변경 시 `RefreshAsync`를 호출하여 캐시를 즉시 갱신합니다.

### ⚠️ 미구현 및 주의 사항
- **출석/포인트 적립**: `StreamerProfile`에는 설정 필드가 존재하고 `md/Research.md`에는 명세되어 있으나, 현재 `Application` 레이어에 이를 처리할 실질적인 **EventHandler가 누락**되어 있습니다. (향후 `ViewerPointEventHandler` 구현 필요)

---

## 4. 모듈별 폴더 구조 요약

- **Domain/Entities**: `StreamerCommand.cs` (스키마 정의)
- **Application/Features/Commands**: 명령어 조회 및 캐싱 로직
- **Application/Features/*/Handlers**: 실질적인 명령어 실행기 (EDA 핸들러)
- **Presentation/Features/Commands**: 명령어 관리 API (Controller)
- **wwwroot/commands.html**: 명령어 관리 웹 UI

---
*본 보고서는 시스템의 확장을 위한 기초 자료로 활용될 수 있습니다.*
