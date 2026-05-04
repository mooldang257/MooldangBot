# 🗺️ MooldangBot (MooldangAPI) 마스터 인덱스 및 아키텍처 가이드

> **[🤖 AI 파트너를 위한 열람 지시어 (System Prompt)]**
> 이 문서는 MooldangBot 프로젝트의 '전체 지도(Master Index)'입니다. 특정 기능을 수정하거나 파악할 때는 이 문서의 요약을 먼저 읽고, **반드시 아래의 상세 도메인 리서치 파일을 추가로 열람**하여 컨텍스트를 확보하십시오. 전체 소스 코드를 무작정 탐색하지 마십시오.
> 
> * **채팅 포인트 & 출석 관련:** `md/ChatPointResearch.md` 참조
> * **오마카세 및 커스텀 명령어 관련:** (예정) `md/CommandResearch.md` 참조
> * **신청곡 큐 (FuncSongListQueues) 및 노래책 관련:** `md/SongQueueResearch.md` 참조

---

## 1. 프로젝트 개요 및 코어 스택

**MooldangBot**은 치지직(CHZZK) 스트리밍 플랫폼과 연동되는 **멀티테넌트 스트리밍 봇 & 대시보드 API 서버**입니다.
* **Tech Stack:** C# .NET 10, EF Core (MariaDB), MediatR (이벤트 드리븐), SignalR, 정적 HTML/JS SPA
* **핵심 아키텍처:** `IUserSession`을 활용한 Global Query Filter 테넌트 격리, `ChzzkChannelWorker` 기반의 백그라운드 WebSocket 봇 워커

---

## 2. 🗂️ 핵심 디렉토리 및 계층 구조 (Layer Summary)

*전체 파일을 나열하지 않고 논리적 계층의 역할만 요약합니다. 수정이 필요한 계층을 파악한 후 실제 코드를 탐색하세요.*

* **진입점 및 설정:** `Program.cs` (DI 구성), `appsettings.json`, `.env` (DotNetEnv 로드)
* **`Controllers/` (REST API 진입점):** 도메인별 분리된 16개 컨트롤러 (`AuthController`, `SongController`, `CommandsController`, `ChatPointController` 등)
* **`Data/` (데이터 접근 계층):** `AppDbContext` (DB/테넌트 격리), `IUserSession` (현재 스트리머 컨텍스트)
* **`Models/` (엔터티 및 DTO):** `CoreStreamerProfiles` (마스터), `ViewerProfile`, `FuncSongListQueues`, `FuncRouletteMain` 등
* **`Features/` (비즈니스 로직 / MediatR Handlers):** Chat, Commands, FuncSongListQueues, FuncRouletteMain, Viewers 도메인별 분리. (예: `ChatMessageReceivedEvent` 구독)
* **`Services/` (백그라운드 & 공유 서비스):** `ChzzkBackgroundService` (봇 매니저), `ChzzkChannelWorker` (웹소켓 연결), `CommandCacheService` (인메모리 캐시)
* **`Hubs/` & `Strategies/`:** `OverlayHub` (SignalR 기반 실시간 브로드캐스트)
* **`wwwroot/` (프론트엔드):** 대시보드 및 OBS 오버레이용 정적 HTML/JS 파일

---

## 3. 🔄 상태 변화 및 트랜잭션 매핑 테이블 (State Mutation)

*MediatR 핸들러 및 기능별 주요 데이터 파이프라인의 입출력 명세입니다. 코드를 수정할 때 아래의 트랜잭션 범위를 준수하세요.*

| 트리거 (Trigger) | 타겟 핸들러 | 읽기 (Read DB / Cache) | 쓰기 (Write DB) | 발행 (Emit Event / SignalR) |
| :--- | :--- | :--- | :--- | :--- |
| **일반 채팅 수신** | `ViewerPointEventHandler` | `ViewerProfile` | `.Points += 1` | `ReceiveChat` (오버레이) |
| **`!출석` 수신** | `ViewerPointEventHandler` | `ViewerProfile`, `CoreStreamerProfiles` | `.Points += 보너스`<br>`.AttendanceCount++` | 봇 채팅 전송 (Reply) |
| **치즈 후원** | `ViewerPointEventHandler` | `ViewerProfile` | `.Points += 후원보너스` | 후원 알림 (SignalR) |
| **`!포인트` 수신** | `CustomCommandEventHandler` | `ViewerProfile` | **없음** | 봇 채팅 전송 (Reply) |
| **오마카세 후원** | `OmakaseEventHandler` | `FuncSongListOmakases` | `.Count += 1` | `RefreshSongAndDashboard` |
| **포인트 룰렛 성공** | `RouletteEventHandler` | `ViewerProfile` | `.Points -= 룰렛비용` | `RouletteTriggered` |
| **방제/카테고리 변경** | `ChannelSettingEventHandler` | `CoreStreamerProfiles` (Role) | **없음** | 치지직 OpenAPI `PATCH` 호출 |

---

## 4. 🛡️ BDD 스타일 예외 및 엣지 케이스 시나리오

*코드 작성 및 리팩토링 시 아래의 테스트 시나리오를 방어하는 로직을 반드시 포함해야 합니다.*

### 4-1. 치지직 WebSocket 프로토콜 수동 파싱 (이중 JSON)
* **Given:** 치지직 서버로부터 `Socket.IO` 이벤트 메시지 `42["CHAT", "{\"profile\":...}"]` 형식을 수신했을 때
* **When:** `ChzzkChannelWorker`가 이벤트를 파싱할 때
* **Then:** `SocketIOClient` 라이브러리를 사용하지 않고, 텍스트를 `Substring(2)`로 자른 뒤 이중 `JsonDocument.Parse`를 수행하여 내부 페이로드를 안전하게 추출해야 한다.

### 4-2. 오마카세 후원 동시성 제어 (낙관적 락)
* **Given:** 다수의 시청자가 동시에 오마카세 후원을 하여 `FuncSongListOmakases`에 동시 업데이트가 발생할 때
* **When:** DB에 `SaveChangesAsync()`를 호출하여 `DbUpdateConcurrencyException`이 발생하면
* **Then:** `[ConcurrencyCheck]` 속성을 기반으로 예외를 캐치하고, 데이터를 Reload하여 카운트를 재계산한 뒤 최대 3회까지 저장을 재시도한다.

### 4-3. 봇 토큰 폴백(Fallback) 우선순위 적용
* **Given:** 봇이 치지직 채팅 채널에 접근하여 메시지를 전송해야 할 때
* **When:** 인증 토큰을 로드하는 과정에서
* **Then:** 1순위 `CoreStreamerProfiles.BotAccessToken`을 확인하고, 없으면 2순위 `SystemSettings`의 전역 봇 토큰을 사용하며, 둘 다 유효하지 않으면 최종적으로 스트리머 본인의 `ChzzkAccessToken`을 사용하도록 분기 처리한다.

### 4-4. HTTP Client 인스턴스 과부하 방지
* **Given:** 다수의 스트리머 채널(Worker)이 치지직 OpenAPI를 호출할 때
* **When:** 각 핸들러나 서비스에서 HTTP 요청을 생성할 때
* **Then:** `new HttpClient()`를 무분별하게 생성하지 않고, 반드시 DI 컨테이너에 등록된 `IHttpClientFactory`를 통해 클라이언트를 주입받아 포트 고갈(Socket Exhaustion)을 방지한다.