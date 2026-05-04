# 🛡️ MooldangBot BDD 예외 및 엣지 케이스 시나리오 (Test Cases)

> **[🤖 AI 파트너를 위한 지시어 (System Prompt)]**
> 이 문서는 MooldangBot 프로젝트의 안정성을 보장하기 위한 테스트 케이스 및 예외 처리 가이드입니다. 기능 구현 및 리팩토링 시, **반드시 아래 명시된 시나리오들을 방어하는 예외 처리(If-Else, Try-Catch, 인증 필터 등)를 코드에 포함**해야 합니다.

---

## 1. 🌐 코어 아키텍처 및 네트워크 (Core & Network)

### 1-1. 치지직 WebSocket 프로토콜 수동 파싱 (이중 JSON)
* **Given:** 치지직 서버로부터 `Socket.IO` 이벤트 메시지 `42["CHAT", "{\"profile\":...}"]` 형식을 수신했을 때
* **When:** `ChzzkChannelWorker`가 이벤트를 파싱하여 페이로드를 추출할 때
* **Then:** `SocketIOClient` 라이브러리를 사용하지 않고, 텍스트를 `Substring(2)`로 자른 뒤 이중 `JsonDocument.Parse`를 수행하여 내부 페이로드를 안전하게 추출해야 한다. 파싱 실패 시 예외를 삼키고 로그를 남긴 후 다음 메시지를 대기한다.

### 1-2. HTTP Client 인스턴스 과부하 (Socket Exhaustion) 방지
* **Given:** 다수의 스트리머 채널(Worker)이 동시에 치지직 OpenAPI를 호출할 때
* **When:** 각 핸들러나 API 클라이언트(`ChzzkApiClient` 등)에서 HTTP 요청을 생성할 때
* **Then:** `new HttpClient()`를 무분별하게 생성하지 않고, 반드시 DI 컨테이너에 등록된 `IHttpClientFactory`를 통해 클라이언트를 주입받아 사용한다.

### 1-3. 봇 토큰 폴백(Fallback) 3단계 우선순위
* **Given:** 봇이 치지직 채팅 채널에 접근하여 응답 메시지를 전송해야 할 때
* **When:** `ChzzkApiClient`가 인증 토큰을 로드하는 과정에서
* **Then:** 1순위 `CoreStreamerProfiles.BotAccessToken`을 확인하고, 유효하지 않으면 2순위 `SystemSettings`의 전역 봇 토큰을 사용하며, 둘 다 실패하면 최종적으로 스트리머 본인의 `ChzzkAccessToken`을 사용하도록 분기 처리한다.

---

## 2. 🔐 보안 및 멀티테넌트 (Security & Multi-tenant)

### 2-1. API 무단 접근 및 테넌트 침범 차단
* **Given:** 악의적인 사용자 또는 인증되지 않은 사용자가 `/api/chatpoint/{chzzkUid}` 엔드포인트에 `GET` 또는 `POST` 요청을 보낼 때
* **When:** 컨트롤러에 요청이 도달하면
* **Then:** `[Authorize]` 어트리뷰트를 통해 인증을 강제하여 401 Unauthorized를 반환하거나, `IUserSession.ChzzkUid`가 요청 URL의 `{chzzkUid}`와 불일치할 경우 403 Forbidden을 반환하여 타 채널의 설정을 읽거나 쓰지 못하게 차단한다.

---

## 3. 🪙 포인트 및 인터랙션 (ChatPoint & Interaction)

### 3-1. 포인트 부족 시 룰렛 스핀 차단
* **Given:** 시청자 A의 잔여 포인트가 500점이고, 설정된 룰렛 1회 비용이 1000점일 때
* **When:** 시청자 A가 채팅창에 `!룰렛` 명령어를 입력하여 `RouletteEventHandler`가 트리거되면
* **Then:** DB의 포인트를 차감하지 않고, `RouletteService`를 호출하지 않으며, 봇이 "포인트가 부족합니다"라는 멘션 채팅을 해당 시청자에게 전송한 뒤 로직을 즉시 종료한다.

### 3-2. 포인트/오마카세 데이터 동시성 경합 (Concurrency Check)
* **Given:** 다수의 시청자가 동시에 오마카세 후원을 하거나, 동일 시청자가 매우 빠른 속도로 연속 채팅을 쳐서 `ViewerProfile` 또는 `FuncSongListOmakases`에 동시 업데이트 트랜잭션이 발생할 때
* **When:** DB에 `SaveChangesAsync()`를 호출하여 `DbUpdateConcurrencyException`이 발생하면
* **Then:** 엔터티에 설정된 `[ConcurrencyCheck]` 속성을 기반으로 예외를 캐치하고, DB에서 최신 값을 Reload하여 더할 포인트/카운트를 재계산한 뒤 최대 3회까지 저장을 재시도한다. 3회 모두 실패 시 에러 로그를 남긴다.

### 3-3. KST 시간대 기반 출석 인정
* **Given:** 서버의 UTC 시간이 `2026-03-24 16:00` (KST 기준 `2026-03-25 01:00`)일 때
* **When:** 시청자가 출석 명령어를 입력하여 `ViewerPointEventHandler`가 실행되면
* **Then:** UTC가 아닌 KST 시간대를 기준으로 날짜가 변경되었는지 확인하고, 오늘 첫 출석이 인정되면 `LastAttendanceAt` 컬럼에 UTC가 아닌 KST 시간을 명시적으로 저장한다.

### 3-4. 외부 API (팔로우 일수) Rate Limit 방어
* **Given:** 봇 응답 포맷에 `{팔로우일수}` 변수가 포함되어 있을 때
* **When:** 짧은 시간에 수십 명의 시청자가 동시에 `!포인트` 명령어를 입력하여 `CustomCommandEventHandler`가 실행되면
* **Then:** 치지직 OpenAPI를 매번 실시간으로 호출하여 Rate Limit(429)에 걸리지 않도록, `IMemoryCache`를 활용하여 시청자별 팔로우 일수 데이터를 일정 시간 캐싱하거나 `ViewerProfile`에 임시 저장하여 외부 API 연속 호출을 방어한다.