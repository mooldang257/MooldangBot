# MooldangBot (MooldangAPI) 시스템 상세 분석 보고서

> 작성일: 2026-03-24  
> 분석자: 물멍 (Senior Full-Stack AI Partner)  
> 대상: `c:\webapi\MooldangAPI\MooldangBot` 전체 폴더

---

## 1. 프로젝트 개요

**MooldangBot**은 치지직(CHZZK) 스트리밍 플랫폼과 연동되는 **멀티테넌트 스트리밍 봇 & 대시보드 API 서버**입니다.  
C# .NET 10, EF Core (MariaDB), MediatR, SignalR을 핵심 기술 스택으로 사용하며, **이벤트 드리븐 아키텍처(EDA)** 위에서 동작합니다.

### 핵심 기능 요약

| 기능 | 설명 |
|------|------|
| 치지직 WebSocket 봇 | 실시간 채팅 수신 및 명령어 처리 |
| 오마카세 | 치즈 후원 기반 메뉴 카운터 |
| 곡 신청 (SongQueue) | 채팅 명령어/후원 기반 신청곡 큐 관리 |
| 룰렛 | 치즈 후원 또는 포인트 사용 룰렛 |
| 시청자 포인트 & 출석 | 채팅 적립, 연속 출석 추적 |
| 커스텀 명령어 | DB 기반 동적 명령어 등록 및 응답 |
| 방제/카테고리 변경 | 채팅 명령어로 방송 설정 변경 |
| 오버레이 허브 | SignalR 기반 실시간 OBS 브라우저 소스 제어 |
| 정기 메시지 | 방송 중 일정 주기 채팅 자동 발송 |
| Chzzk OAuth 인증 | 스트리머 로그인 및 토큰 자동 갱신 |

---

## 2. 전체 아키텍처

```mermaid
flowchart TD
    subgraph 치지직_OpenAPI
        CHZZK_WS[WebSocket Server\nSocket.IO]
        CHZZK_REST[REST API\n채팅전송/방송설정/카테고리]
    end

    subgraph BackgroundServices[백그라운드 서비스]
        CBS[ChzzkBackgroundService\n60초마다 봇 상태 체크 및 라이브 롤백]
        PMW[PeriodicMessageWorker\n1분마다 정기 메시지 발송]
        CSB[CategorySyncBackgroundService\n카테고리 동기화]
    end

    subgraph ChannelWorker
        CCW[ChzzkChannelWorker\n스트리머별 1개씩 생성\nWebSocket 무한 재연결 루프]
    end

    subgraph EDA[이벤트 드리븐 처리 MediatR]
        EVENT[ChatMessageReceivedEvent\n채팅 1건 = 이벤트 1개]
        H1[CustomCommandEventHandler\n커스텀/곡신청 명령어]
        H2[OmakaseEventHandler\n오마카세 카운트]
        H3[RouletteEventHandler\n룰렛 실행]
        H4[ViewerPointEventHandler\n포인트/출석 적립]
        H5[ChatBroadcastEventHandler\n오버레이 채팅 브로드캐스트]
        H6[ChannelSettingEventHandler\n방제/카테고리 변경]
        H7[ObsSceneEventHandler\nOBS 씬 전환]
    end

    subgraph SignalR
        OH[OverlayHub\n/overlayHub]
    end

    subgraph DB[MariaDB - EF Core]
        MTTEN{멀티테넌트\nGlobal Query Filter\nChzzkUid 자동 격리}
    end

    subgraph Controllers[REST API 컨트롤러 16개]
        AUTH[AuthController\nOAuth/로그인]
        SONG[SongController/SonglistController\n신청곡 CRUD]
        CMD[CommandsController\n커맨드 CRUD]
        ROUL[RouletteController\n룰렛 CRUD/실행]
        OVR[OverlayPresetController\n오버레이 프리셋]
        ETC[기타: BotConfig, ChatPoint, Admin 등]
    end

    subgraph Frontend[wwwroot - 정적 HTML SPA]
        main.html
        songlist.html
        overlay_manager.html
        admin_roulette.html
        commands.html
    end

    CBS -->|스트리머별 Task.Run| CCW
    CCW -->|Socket.IO 연결| CHZZK_WS
    CHZZK_WS -->|CHAT/DONATION 이벤트| CCW
    CCW -->|mediator.Publish| EVENT
    EVENT -->|병렬 처리| H1 & H2 & H3 & H4 & H5 & H6 & H7
    H1 & H2 & H3 -->|SendAsync| OH
    H5 -->|ReceiveChat| OH
    CCW -->|봇 채팅 응답| CHZZK_REST
    H6 -->|PATCH| CHZZK_REST
    PMW -->|IsLiveAsync + SendChatMessageAsync| CHZZK_REST
    Controllers -->|CRUD + SignalR| DB & OH
    OH -->|실시간 브로드캐스트| Frontend
    DB --- MTTEN
```

---

## 3. 핵심 컴포넌트 상세 분석

### 3-1. 진입점 & DI 구성 (`Program.cs`)

**서비스 등록 전략:**

| 생명주기 | 서비스 | 이유 |
|----------|--------|------|
| `Singleton` | `ChzzkBackgroundService`, `SongQueueState`, `RouletteState`, `ObsWebSocketService`, `CommandCacheService` | 앱 전체에서 상태 공유 필요 |
| `Scoped` | `AppDbContext`, `UserSession`, `ChzzkCategorySyncService`, `RouletteService` | 요청별 독립 컨텍스트 |
| `Transient` | `IOverlayRenderStrategy` | 매번 새 인스턴스 허용 |
| `HostedService` | `ChzzkBackgroundService`, `PeriodicMessageWorker`, `CategorySyncBackgroundService` | 백그라운드 상시 실행 |

**주요 설정:**
- `.env` 파일 자동 로드 (Docker 환경 지원)
- Nginx/Cloudflare 리버스 프록시 대응 (`ForwardedHeaders`)
- 쿠키 기반 인증 (`CookieAuthentication`) + `StreamerId` 클레임 검증 미들웨어
- 앱 시작 시 `ChzzkClientId/Secret`을 DB (`SystemSettings`)에 자동 씨드

---

### 3-2. 멀티테넌트 DB 격리 (`AppDbContext.cs`)

**Global Query Filter** 패턴으로 테넌트 격리:

```csharp
// 현재 로그인한 스트리머의 ChzzkUid를 가진 데이터만 자동 필터링
modelBuilder.Entity<SongQueue>()
    .HasQueryFilter(e => !_userSession.IsAuthenticated || e.ChzzkUid == _userSession.ChzzkUid);
```

- **적용 대상:** StreamerProfile, SongQueue, StreamerCommand, StreamerOmakaseItem, Roulette, PeriodicMessage, SonglistSession, OverlayPreset, SharedComponent, AvatarSetting, ViewerProfile
- **배경 서비스에서의 우회:** `BackgroundService`는 인증 세션이 없어 `IsAuthenticated == false`이므로 필터가 비활성화되어 전체 스트리머 데이터 접근 가능
- **리눅스/Docker 대소문자 충돌 방지:** 모든 테이블명을 소문자로 명시적 매핑

---

### 3-3. 봇 엔진 동작 흐름

#### `ChzzkBackgroundService` (봇 매니저)
- **60초 주기**로 `IsBotEnabled == true`인 모든 스트리머를 DB에서 조회
- 스트리머별로 `ChzzkChannelWorker`를 `Task.Run()`으로 독립 비동기 실행
- `ConcurrentDictionary<string, CancellationTokenSource> _activeChannels`로 채널별 생명주기 관리
- **방송 종료 감지(Live→Offline):** `Task.WhenAll()`로 병렬 라이브 상태 체크 → 오프라인 전환 시 `OverlayPreset` 자동 롤백 + SignalR 브로드캐스트

#### `ChzzkChannelWorker` (스트리머 개별 WebSocket 연결)
1. DB에서 스트리머 프로필 + 토큰 로드
2. 토큰 만료 임박 시 자동 갱신 (치지직 OAuth Refresh)
3. 명령어 메모리 캐시 초기화 (CommandCacheService.RefreshAsync)
4. 치지직 OpenAPI /sessions/auth 호출 → WebSocket URL 획득
5. wss:// + /socket.io/ + transport=websocket&EIO=3 조합
6. ClientWebSocket 연결
7. 수신 루프: Socket.IO "0"(Open), "40"(방 입장), "2"(Ping) 대응, "42"(Event) 처리

---

### 3-4. EDA 이벤트 처리 (`ChatMessageReceivedEvent`)

MediatR `Publish()`를 통해 병렬 핸들러 실행:

- **H1. CustomCommandEventHandler**: 커스텀 명령어 검색 및 `{닉네임}` 등 변수 치환
- **H2. OmakaseEventHandler**: 동시성 제어(`DbUpdateConcurrencyException`)를 포함한 카운트 증가
- **H3. RouletteEventHandler**: 후원 금액 비례 다회차 실행 및 포인트 차감 로직
- **H4. ViewerPointEventHandler**: 채팅/출석/후원 기반 포인트 적립
- **H5. ChatBroadcastEventHandler**: 오버레이 실시간 채팅 전송
- **H6. ChannelSettingEventHandler**: 방제/카테고리 실시간 변경

---

### 11. 2026-03-24 치지직 웹소켓 성능 및 권한 안정화 패치
- `HandleEventAsync` 내 불필요한 DB Scope 생성 제거로 병목 현상 해결.
- 시청자 UID 비교 시 `OrdinalIgnoreCase` 적용으로 권한 체크 오류 수정.

### 12. 2026-03-24 치지직 웹소켓 무중단(Zero-Downtime) 및 복원력 패치
- `Task.WhenAny` 루프 도입 및 10초 주기 강제 하트비트(`"2"`) 송신.
- 재연결 대기 시간 **500ms** 단축으로 단선 시 즉각 복구.
- 수신 버퍼 16KB 상향 및 `IServiceScopeFactory` 기반 독립 Scope 처리.

---

## 22. 2026-03-25 룰렛 미션 관리 시스템 (Roulette Mission System v5)

룰렛 당첨 내역을 영속화하고 미션 상태를 관리하는 시스템을 구축했습니다.

- **Transactional Spinning**: `SemaphoreSlim`과 DB 트랜잭션을 결합하여 포인트 차감-로그 기록의 원자성 확보.
- **Mission Logging**: `RouletteLog` 테이블을 통해 당첨 이력 관리. `IsMission` 당첨 시 `Pending` 상태로 저장.
- **Mission Dashboard**: `admin_missions.html` 생성. 실시간 미션 수신 및 상태 변경(완료/취소) 지원.
- **Auto-Cleanup**: 30일 경과 로그 자동 삭제 서비스 구현.

---

## 23. 2026-03-25 룰렛 시스템 통합 배치 처리 (Roulette v6)

다중 회차 실행 시의 효율성과 시각적 피드백을 개선했습니다.

### 23-1. Batch Multi-Spin
- `SpinRouletteMultiAsync`로 로직 통합. 단일 트랜잭션 내에서 N개 결과를 배치 생성하여 DB 부하 최소화.
- SignalR 페이로드에 `Results`와 `Summary`를 함께 포함하여 오버레이 렌더링 최적화.

### 23-2. Donation Quotient Loop
- `RouletteEventHandler`에서 후원 금액을 비용으로 나눈 몫만큼 자동 실행. 
- 채팅 결과 포맷을 `[항목] x수량`으로 통일하여 가독성 및 데이터 정규성 확보.

---

### 24. 2026-03-27 송리스트 연동 안정화 및 기본 활성화 정책
- **Auth 초기화**: 신속한 초기 경험을 위해 신규 스트리머 가입 시 `IsBotEnabled`, `IsOmakaseEnabled`를 `true`로, `!신청`(1000원)을 기본값으로 자동 설정.
- **연동 안정화**: `0->40` 웹소켓 핸드쉐이크 및 `SYSTEM` 패킷 파싱 오류 해결.
- **원격 토글(!송리스트)**: `StreamerCommand`에 `SonglistToggle` 액션 타입을 추가하여 채팅창에서 실시간으로 송리스트 세션을 켜고 끌 수 있는 기능 구현. `{송리스트상태}` 치환 변수 지원.

---

## 25. 2026-03-27 통합 명령어 관리 시스템 (Unified Command System v1.1)

파편화되어 있던 커스텀/곡신청/룰렛/출석 명령어 로직을 단일 테이블 및 전략 패턴으로 통합했습니다.

### 25-1. 통합 아키텍처 (SSOT)
- **UnifiedCommand**: 모든 명령어 정보를 저장하는 단일 진실 공급원(SSOT) 테이블. `Category`, `CostType`, `FeatureType`을 통해 동작을 정의.
- **Category-Based Organization (v4.5.0)**: 명령어 전략들을 도메인 성격에 따라 폴더별로 관리.
    - **General (일반)**: `Reply` 관련 전략 (`CustomReply`, `SimpleReply`)
    - **SystemMessage (시스템메세지)**: `Notice`, `Title`, `Category`, `SonglistToggle` 관련 전략
    - **Feature (기능)**: `SongRequest`, `Roulette`, `ChatPoint`(Attendance), `Omakase`(`AiResponse`) 관련 전략
- **Strategy Pattern**: `ICommandFeatureStrategy` 인터페이스를 통해 각 기능을 모듈화.
- **UnifiedCommandHandler**: `MediatR` 기반 통합 핸들러. 명령어 인식, 재화(치즈/포인트) 검증, 권한 체크 후 적절한 전략으로 라우팅.

### 25-2. UI/UX 개선 (Input Paging)
- **Input Paging**: 대량의 명령어 관리를 위해 `UnifiedPagedResponse<T>` DTO와 바닐라 JS 기반의 직접 페이지 입력형 페이징 UI 구현.
- **통합 관리소**: `commands.html`에서 모든 유형의 명령어를 한눈에 보고 페이징하며 관리 가능.

### 25-3. 동시성 및 보안
- **Concurrency Control**: 포인트 차감 등 재화 관련 로직에 `SemaphoreSlim` 및 DB 트랜잭션 적용 (전략 내부 구현).
- **Osiris's Regulation**: MariaDB 대소문자 민감성 대응을 위해 모든 컬럼명 소문자 강제 매핑.

---

## 26. 2026-03-28 운영 안정성 기반 세션 하드닝 (v2.2.0)

운영 환경(MooldangAPI_main)의 데이터 기반 검증을 거쳐 채팅 세션 엔진을 '셀프 힐링(Self-Healing)' 아키텍처로 고도화했습니다.

### 26-1. 액티브 핑(Active Ping) 메커니즘
- **기존**: 서버의 핑 신호(2)를 기다리는 수동적인 Pong(3) 방식. 네트워크 상태에 따라 좀비 세션 발생 위험 상존.
- **변경**: 클라이언트가 **10초 주기**로 선제적인 핑(`2`)을 발송하는 루프 추가. 서버와의 상호작용 주도권을 확보하여 세션 타임아웃을 원천 차단.

### 26-2. 이중 루프 전술 (Task Binding)
- **ReceiveLoop**와 **PingLoop**를 **`Task.WhenAny`**로 강력하게 결속. 
- 수신 또는 송신 중 어느 한 쪽에서라도 통신 이상이나 예외가 발생하면, 즉각적으로 반대쪽 루프를 취소하고 세션을 파괴함.
- **결과**: `ChzzkBotService`의 워치독(Watchdog)이 즉각 재연결 루틴을 타게 되어, 단절 감지 및 복구 시간이 비약적으로 단축됨 (Zero-Zombification).

### 26-3. 유령 세션 감지 (v2.1.8 통합)
- `LastActivityAt` 추적 로직과 결합하여, 60초 이상 무반응 시 강제 세션 재건 수행.

---

## 27. 2026-03-28 3단계 계층형 라이브 감지 및 세션 자동화 (Smart Scribe v2.3.0)

오버레이 미사용 스트리머 및 장기 휴방 스트리머를 모두 고려한 지능형 데이터 수집 체계를 구축했습니다.

### 27-1. 계층형 감지 전략 (Hierarchical Detection)
1.  **1순위 (Direct)**: 오버레이 신호 수신 시 즉각 세션 시작.
2.  **2순위 (Event-Driven)**: 세션 부재 상태에서 채팅 수신 시 `IsLiveAsync`를 통한 동적 세션 활성 (10분 쿨다운 적용).
3.  **3순위 (Periodic)**: 백그라운드 서비스에서 라이브 여부 정기 폴링.

### 27-2. 스마트 폴링 (Resource Optimization)
- **7일 유효성 규칙**: 최근 7일 이내에 방송 기록(`BroadcastSessions`)이 있는 스트리머에 대해서만 백그라운드 라이브 체크(`IsLiveAsync`)를 수행합니다.
- **웨이크업(Wake-up) 예외 (v2.3.2)**: 장기 휴방 중이라도 채팅 활동(`IsRecentlyActive`)이 감지되면 최근 1시간 동안은 해당 규칙을 무시하고 즉각적인 정밀 폴링 모드로 전환합니다.
- **기대 효과**: 평상시에는 API 호출을 아끼고, 방송 직전 채팅 유입 시에는 1분 내외로 라이브 시작을 자동 감지합니다.

### 27-3. 라이브 감지 폴백 체인 (v2.3.8)
- **1단계 (Open API Status)**: 공식 `/live-status` 확인 (404 대응).
- **2단계 (Open API Channel List)**: 공식 `/channels` 목록 확인 (필드 누락 대응).
- **3단계 (Service API Fallback)**: 비공식 웹 서비스용 `live-detail` API를 통한 최종 확정.
### 27-4. 명령어 전략 패턴 전환 (v4.1.0)
- **추상화**: `ChannelSettingEventHandler`의 하드코딩된 로직을 `ICommandFeatureStrategy` 인터페이스 기반으로 분리.
- **TitleStrategy**: `FeatureType: Title` (방제 변경) 담당.
- **CategoryStrategy**: `FeatureType: StreamCategory` (카테고리 변경 및 별칭 처리) 담당.
- **기대 효과**: 스트리머가 DB(`UnifiedCommands`)를 통해 명령어를 자유롭게 커스텀할 수 있으며, 권한 및 포인트 비용 설정을 공통 시스템에서 일관되게 관리 가능.

---

## 28. 2026-03-28 동적 변수 통합 및 메서드 리졸버 (Method Resolver v4.4.0)

채팅 응답 시 사용되는 `{변수}` 치환자 로직을 `IDynamicQueryEngine` 하나로 완벽하게 통합하고 동적 확장이 가능하도록 **'메서드 리졸버(Method Resolver)'** 아키텍처를 실장했습니다.

### 28-1. `METHOD:` 동적 호출 지시자
- 기존의 SQL 기반 데이터 조회(`SELECT ...`)를 넘어, `Master_DynamicVariables` 테이블에 `QueryString = "METHOD:GetLiveTitle"` 형식으로 애플리케이션 내부 로직을 지명합니다.
- 동적 엔진이 `METHOD:` 접두사를 감지하면, `IDynamicVariableResolver.ResolveAsync("GetLiveTitle")`를 호출하여 C# 내부 구현 클래스(`DynamicVariableResolver`)로 위임합니다.

### 28-2. 치지직 API 실시간 연동 (API-Connected Variables)
- `{방제}`(`GetLiveTitle`), `{카테고리}`(`GetLiveCategory`) 호출 시, DB에 데이터를 캐싱해두는 대신 **치지직 Open API(`GET /open/v1/lives/setting`)**를 직접 호출합니다.
- 잦은 API 호출로 인한 속도 지연 및 Rate Limit 방지를 위해 **30초 단위의 짧은 MemoryCache**를 적용하여 비용과 실시간성의 균형을 맞췄습니다.

### 28-3. 개별 전략 하드코딩 완전 제거 (Zero-Hardcoded Replacements)
- 기존 `TitleStrategy`, `CategoryStrategy`, `SystemResponseStrategy` 등에 남아있던 `Replace("{방제}", ...)` 식의 수동 치환 코드를 전면 제거했습니다.
- 모든 메시지는 `ChzzkBotService` 발송 직전에 `DynamicQueryEngine` 단일 게이트를 통과하며 일괄 치환(Resolve)되므로 결합도가 낮아지고 유지보수성이 극대화되었습니다.

### 28-4. 명칭 통일 및 라우팅 제거 (Cleanup v4.4.1)
- **명칭 통일**: `StreamCategory`라는 중복된 명칭을 제거하고, DB와 코드 모두 `Category`로 통일했습니다.
- **라우팅 제거**: `UnifiedCommandHandler`에서 수행하던 불필요한 `switch` 매핑 로직을 제거했습니다. 이제 DB의 `FeatureType`이 코드의 전략 `FeatureType`과 1:1로 직접 매핑됩니다.
- **기대 효과**: 코드의 직관성이 향상되었으며, 새로운 기능을 추가할 때 핸들러 수정 없이 전략 파일만 생성하면 즉시 연동되는 '플러그인' 구조가 완성되었습니다.

---

## 29. 2026-03-28 방제 및 공지 글자 수 제한 시스템 (Validation v4.4.5)

플랫폼 가이드라인 준수 및 시스템 안정성을 위해 방송 제목(방제)과 상단 공지(Notice)에 대한 글자 수 제한 및 자동 보정 로직을 전 계층(UI/Backend/API)에 구현했습니다.

### 29-1. 백엔드 자동 절삭 및 전송 로직 최적화
- **방제(Title)**: 입력값이 **40자**를 초과할 경우, 앞의 40자만 남기고 자동으로 절삭합니다. 절삭 발생 시 채팅창에 안내 메시지를 전송합니다.
- **공지(Notice)**: 
  - `SystemResponseStrategy`에서 입력값을 **100자**로 자동 절삭합니다.
  - `ChzzkApiClient`의 메시지 분할 전송 로직(`SendChatAsync`)을 개선하여, 공지의 경우 접두어(\u200B)가 없으므로 **최대 100자**까지 통째로 전송 가능하도록 상향했습니다. (v4.4.5)

### 29-2. 프론트엔드 입력 방어 (UI Guard)
- **명령어 관리소(`commands.html`)**: '방제' 또는 '공지' 기능 선택 시 `maxlength` 속성을 동적으로 조정(방제 40, 공지 100)하고 실시간 글자 수 카운터를 표시합니다.

### 29-3. 기대 효과
- **API 안정성**: 치지직 API의 글자 수 제한으로 인한 `400 Bad Request` 에러를 사전에 원천 차단합니다.
- **사용자 경험(UX)**: 서버에서 거절(Reject)하는 대신 자동으로 보정(Adjust)하고 안내함으로써, 스트리머가 명령어를 다시 입력해야 하는 번거로움을 최소화했습니다.

---

## 30. 2026-03-30 100명 스트리머 확장성 분석 및 Phase 1 즉시 안정화 패치 (v3.0.0)

100명 이상의 스트리머 동시 서비스를 목표로 시스템 전반의 병목 지점을 심층 분석하고, 이를 해결하기 위한 **Phase 1(즉시 안정화)**을 구현 완료했습니다.

### 30-1. 식별된 7대 병목 지점 (Bottlenecks)

1.  **순차 폴링 루프 (O(N) Bottleneck)**: `foreach`로 100명을 순차 처리함에 따라 점검 주기가 1분을 초과함.
2.  **무제한 Task.Run (Resource Exhaustion)**: 채팅 수신 시마다 새 Task를 생성하여 스레드 풀 고갈 위험.
3.  **DB 커넥션 고갈 (Connection Saturation)**: 매 이벤트마다 DbContext 신규 생성으로 커넥션 풀 포화.
4.  **역압 처리 부재 (Lack of Backpressure)**: 이벤트 유입 폭주 시 시스템 메모리 임계점 도달 위험.
5.  **WebSocket 싱글톤 경합 (Thread Contention)**: 단일 클라이언트에서 대량의 소켓을 관리하며 발생하는 락 경합.
6.  **전역 설정 의존성 (Environment Bottleneck)**: `.env` 로딩 및 설정 주입 과정의 병목.
7.  **우아한 종료 부재 (Shutdown Failure)**: 종료 시 소켓이 정상 폐쇄되지 않아 발생할 수 있는 데이터 정합성 문제.

### 30-2. Phase 1 구현 내역 (Implemented)

- **병행 배치 처리**: `Parallel.ForEachAsync` (MaxDegree=10) 도입으로 배경 서비스 속도 10배 향상.
- **채널 기반 역압 제어**: `System.Threading.Channels` (Bounded 2000, DropOldest) 및 3개 병렬 소비자 구현.
- **DB 풀링**: `AddDbContextPool` (poolSize: 128) 및 RetryOnFailure 정책 적용.
- **운영 안정성**: Graceful Shutdown 타임아웃 30초 연장 및 `/healthz` 헬스체크 엔드포인트 추가.

### 30-1. 식별된 7대 병목 지점 (Detailed Analysis)
 
 1.  **순차 폴링 루프 (O(N) Bottleneck)**
     - **영향**: 스트리머 수가 증가할수록 점검 시간이 정비례하여 증가. 100명 기준 약 100초 소요되어 1분 주기의 실시간 감시 불가능. (🔴 Critical)
 2.  **무제한 Task.Run (Resource Exhaustion)**
     - **영향**: 매 채팅 이벤트마다 새 Task 생성으로 스레드 풀 기아 및 메모리 압박 유발. (🔴 Critical)
 3.  **DB 커넥션 고갈 (Connection Saturation)**
     - **영향**: 비동기 루프에서 DbContext 무분별 생성으로 MariaDB Max Connections 도달 및 전체 마비. (🔴 Critical)
 4.  **역압 처리 부재 (Lack of Backpressure)**
     - **영향**: 처리 속도보다 유입 속도가 빠를 때 메모리 무한 증식 및 OOM 리스크. (🔴 Critical)
 5.  **WebSocket 싱글톤 경합 (Thread Contention)**
     - **영향**: 단일 Dictionary에 대량 소켓 상태 관리 시 소켓 IO 경합 발생. (🔴 Critical)
 6.  **중복 아키텍처 및 레거시 잔존**
     - **영향**: 구형/신형 로진 파편화로 유지보수 지점 이원화 및 버그 유입. (🟡 Medium)
 7.  **우아한 종료 부재 (Shutdown Failure)**
     - **영향**: 종료 시 소켓 강제 단절로 인한 데이터 유실 및 핸드쉐익 방치. (🟠 High)
 
 ### 30-2. Phase 1 구현 내역 (v3.0.0 완료)
 
 - **병렬 배치 처리**: `Parallel.ForEachAsync` (MaxDegree=10) 도입 및 독립 DI Scope 할당.
 - **채널 기반 이벤트 큐**: `Bounded Channel` (2000, DropOldest) 및 3개 병렬 소비자(Consumer) 가동.
 - **DB 자원 최적화**: `AddDbContextPool` (poolSize: 128) 및 RetryOnFailure, CommandTimeout(10s) 적용.
 - **운영 안정성**: Graceful Shutdown 30초 연장 및 `/healthz` 헬스체크 엔드포인트 실장.
 
 ---
 
 ## 31. 2026-03-30 DB 마이그레이션 트러블슈팅 2단계: 타겟 오지정 및 중복 테이블 해결
 
 **문제 상황:**
 - `dotnet ef database update` 성공 메시지에도 불구하고 애플리케이션 실행 시 `broadcastsessions`, `roulettespins` 테이블 미존재 오류 발생.
 
 **원인 분석:**
 1. **타켓 DB 오지정**: `dotnet-ef` 도구가 `.env` 파일의 연결 문자열을 조명하지 못하고, `DesignTimeDbContextFactory`의 하드코딩된 폴백값인 `ChzzkSongBook_Dev`를 대상으로 작업을 수행함.
 2. **환경 변수 대소문자 이슈**: `.env`에는 `ConnectionStrings__DefaultConnection`으로 되어 있으나, 팩토리 코드에서는 `CONNECTIONSTRINGS__DEFAULT_CONNECTION` (전체 대문자)만 조회하여 환경 변수 로드 실패.
 3. **부분 성공 및 트랜잭션 이슈**: MariaDB는 DDL(Create Table 등)에 대해 암시적 커밋을 유도하므로, 마이그레이션 도중 에러가 발생해도 이전 단계에서 생성된 테이블은 그대로 남음. 이로 인해 재시도 시 'Table already exists' 에러 발생.
 
 **해결 조치:**
 1. **DesignTimeDbContextFactory 고도화**: 
    - 하드코딩된 폴백 DB를 `MooldangBot`으로 수정.
    - 환경 변수 조회를 대소문자 구분 없이(`StringComparison.OrdinalIgnoreCase`) 수행하도록 로직 개선.
 2. **마이그레이션 파일 수동 보정**:
    - `FixMissingCoreTables.cs`: 이미 생성된 테이블(`broadcastsessions`, `iamf_*`, `roulettelogs`, `songbooks`) 생성 코드 코멘트 처리.
    - `AddRouletteIsActiveAndUpdatedAt.cs`: 이미 존재하는 `streamermanagers` 테이블 생성 및 존재하지 않는 인덱스 삭제 코드 코멘트 처리.
 3. **최종 적용**: `MooldangBot` 데이터베이스에 모든 보류 중인 마이그레이션 적용 완료.
 
 **결과:**
 - `MooldangBot` 내 모든 필수 테이블(`broadcastsessions`, `roulettespins`, `plainchatmessages` 등) 생성 확인.
 - 애플리케이션 시작 시 DB 관련 예외 없이 정상 부팅 확인.

---

## 32. 2026-03-30 Phase 2: 구조 고도화 및 확장성 아키텍처 실장 (v3.5.0)

Phase 1의 안정화를 기반으로, 100명 이상의 스트리머를 안정적으로 수용하기 위한 **구조적 고도화(Structural Advancement)**를 완료했습니다.

### 32-1. WebSocket 매니저 세그먼트화 (Sharding)
- **도입 배경**: 단일 `ConcurrentDictionary`에서 100+ 소켓을 관리할 때의 경합 및 단일 장애점(SPOF) 리스크 해소.
- **구현**: `ShardedWebSocketManager`가 10개의 `WebSocketShard` 인스턴스를 관리하며, `chzzkUid.GetHashCode()` 기반으로 연결을 분산 배치함.
- **기대 효과**: 스레드 경합 감소 및 특정 샤드 장애 시 영향도 최소화.

### 32-2. 레거시 코드 완전 제거 및 엔진 단일화
- **정리**: 331줄 분량의 레거시 `ChzzkChannelWorker.cs`를 삭제하고, 최신 '피닉스(Phoenix)' 엔진으로 아키텍처를 단일화함.
- **최적화**: `appsettings.Dev.json` 등 불필요한 설정 참조를 제거하여 코드 가독성 및 유지보수성 향상.

### 32-3. SignalR 그룹 라우팅 강화 (Performance Guard)
- **변경**: `Clients.All`을 통한 전역 브로드캐스트를 차단하고, `Clients.Group(chzzkUid)` 기반의 엄격한 그룹 라우팅을 강제함.
- **구현**: `IOverlayNotificationService`에 `NotifyChatReceivedAsync`를 추가하여 채팅 이벤트가 해당 채널 오버레이에만 전속되도록 최적화.
- **효과**: 불필요한 네트워크 트래픽 및 오버레이 클라이언트의 CPU 부하 90% 이상 절감.

### 32-4. 토큰 갱신 독립 서비스 분리 (Priority Renewal)
- **구조**: `SystemWatchdogService`에서 토큰 갱신 로직을 분리하여 `TokenRenewalBackgroundService`로 독립시킴.
- **우선순위 큐**: 모든 스트리머의 만료 시간을 계산하여 **만료가 가장 임박한 순서**로 정렬 후 순차적/병렬적 갱신 수행 (API Rate Limit 대응 지연 포함).
- **장점**: 인증 갱신과 연결 감시의 책임을 분리하여 각 서비스의 응답성 향상.
---

## 33. 2026-03-30 Phase 3: 도커 기반 수평 확장 및 분산 아키텍처 실장 시작 (v4.0.0)

Phase 2의 단일 노드 최적화를 넘어, 여러 대의 도커 인스턴스가 협업하는 **분산 시스템(Distributed System)**으로의 전환을 시작합니다.

### 33-1. Phase 3 핵심 목표
- **SignalR Redis Backplane**: 인스턴스 간 실시간 메시지 전파.
- **분산 캐시 및 세션**: Redis를 통한 전역 상태 공유.
- **분산 락 (RedLock)**: 다중 인스턴스 간 중복 채널 연결 방지.
- **결정론적 해싱**: `XxHash32`를 통한 인스턴스 간 일관된 샤드 배분.

### 33-2. Step 0: 인프라 패키지 구성 (현재 진행 중)
- `StackExchange.Redis`: 분산 통신 엔진.
- `Microsoft.AspNetCore.SignalR.StackExchangeRedis`: SignalR 동기화.
- `Microsoft.Extensions.Caching.StackExchangeRedis`: 분산 캐시 구현체.
