# ChzzkAPI 코딩 가이드 (Coding Guide)

본 문서는 `MooldangBot.ChzzkAPI` 게이트웨이 서비스의 유지보수 및 확장을 위한 핵심 개발 원칙과 가이드를 제공합니다.

---

## 1. 핵심 개발 철학 (Core Philosophy)

### 🚨 치지직 공식 문서 준수 (Strict Documentation Adherence)
외부 치지직 API와의 모든 통신은 프로젝트 내 `API Documentation` 폴더에 위치한 공식 명세(Markdown)를 철저히 준수해야 합니다.
- **WebSocket 패킷 구조**: `Session.md`에 정의된 필드 명칭과 계층 구조를 최우선으로 반영합니다.
- **REST API 호출**: 엔드포인트 경로, 쿼리 파라미터, HTTP 메서드는 공식 가이드를 따릅니다.
- **데이터 모델링**: `ChzzkChatPayload` 등 수신 모델은 공식 문서의 구조를 기반으로 설계하되, 실제 데이터에서 발견된 예외 케이스(예: 중첩된 `userRoleCode`)는 방어적 로직으로 보완합니다.

---

## 2. 이벤트 파이프라인 (Event Pipeline)

### WebSocket $\rightarrow$ 게이트웨이 $\rightarrow$ RabbitMQ
게이트웨이는 치지직으로부터 수신한 데이터를 정규화하여 메인 앱으로 중계하는 역할을 합니다.

1.  **데이터 수신**: `WebSocketShard`가 Socket.IO 프로토콜을 통해 `JsonElement` 형태로 데이터를 받습니다.
2.  **데이터 정규화**: `ChzzkChatPayload` 모델을 통해 역직렬화합니다. 이때 루트 필드와 `profile` 내부 필드를 모두 체크하여 데이터 유실을 방지합니다.
2.5 **봇 자가 응답 방어**: `BOT_CHZZK_UID`와 발신자 ID를 대조하여, 봇 자신의 메시지인 경우 RabbitMQ 발행을 중단합니다. (무한 루프 및 중복 적립 방지)
3.  **다형성 이벤트 생성**: `ChzzkEventType`에 따라 `ChzzkChatEvent`, `ChzzkDonationEvent` 등으로 변환합니다.
4.  **엔벨로프 래핑**: `ChzzkEventEnvelope`에 감싸서 RabbitMQ 익스체인지(`mooldang.chzzk.chat`)로 사출합니다.

---

## 3. 통신 및 메시징 규칙

### RabbitMQ 익스체인지 전략
- **Exchange Name**: `mooldang.chzzk.chat` (Topic 방식)
- **Routing Key**: 이벤트 타입에 따라 구분 (예: `chat`, `donation`)

### RPC 명령어 수신 (Inbound Command)
메인 앱에서 게이트웨이로 보내는 제어 명령(채팅 발송, 세션 재연결 등)은 `ChzzkCommandConsumer`를 통해 수신합니다.
- **명령어 종류**: `SendMessageCommand`, `ReconnectCommand` 등
- **집행**: 수신된 명령어는 `ShardedWebSocketManager`를 통해 해당 샤드나 클라이언트로 전달되어 실행됩니다.

---

## 4. 코딩 컨벤션 및 로깅

### 로깅 (Logging)
- **Trace/Debug**: 원본 JSON 데이터(`Raw Payload`) 및 상세 처리 과정
- **Information**: 주요 상태 변경 (재연결 성공, 후원 수신, 명령어 매칭)
- **Error**: 외부 서버 연결 실패, 인증 오류 등 (로그에 URL 및 상태 코드 포함)

### 동시성 제어 (Concurrency)
- 다수의 샤드가 병렬로 작동하므로, 공유 자원 접근 시 `ConcurrentDictionary` 또는 적절한 동기화 객체를 사용해야 합니다.
- 비동기 메서드 사용 시 반드시 `CancellationToken`을 전달하여 우아한 종료(Graceful Shutdown)를 지원합니다.

---

## 5. 유지보수 체크리스트
- [ ] 신규 이벤트 타입을 추가할 때 `ChzzkEventType` 열거형 및 `Contracts` 프로젝트의 모델을 확인했는가?
- [ ] 공식 문서 내용이 변경되었을 때, `API Documentation`을 업데이트하고 관련 모델을 수정했는가?
- [ ] **봇 자가 응답 방어**: `BOT_CHZZK_UID` 환경 변수가 유효하며, 새로운 이벤트 처리 로직에서도 필터링이 적용되는가?
- [ ] RabbitMQ 연결 설정 및 익스체인지 이름이 `appsettings.json`과 일치하는가?
