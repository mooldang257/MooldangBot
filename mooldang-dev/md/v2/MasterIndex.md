# 🗺️ MooldangBot (MooldangAPI) Master Index

> **[🤖 AI 파트너를 위한 열람 지시어 (System Prompt)]**
> 이 문서는 프로젝트의 '전체 지도'입니다. 전체 소스 코드를 탐색하기 전에 이 문서의 계층 구조와 전역 규칙을 먼저 숙지하십시오. 특정 도메인의 기능을 추가/수정할 때는 **반드시 아래 명시된 상세 도메인 리서치 파일을 추가로 열람**해야 합니다.
> 
> * **채팅 포인트 & 출석 & 룰렛 도메인:** `md/ChatPoint_Domain.md` 참조
> * *(기타 도메인 파일은 추후 추가 예정)*

---

## 1. 프로젝트 코어 아키텍처
**MooldangBot**은 치지직(CHZZK) 연동 멀티테넌트 스트리밍 봇 & 대시보드 API 서버입니다.
* **Tech Stack:** C# .NET 10, EF Core (MariaDB), MediatR (이벤트 드리븐), SignalR
* **데이터 격리:** `IUserSession`을 활용한 EF Core Global Query Filter 테넌트 격리.
* **봇 엔진:** `ChzzkChannelWorker` 기반의 백그라운드 WebSocket 개별 워커 구조.

## 2. 핵심 디렉토리 및 계층 구조
* **`Controllers/`:** 도메인별 분리된 REST API 엔드포인트 (`ChatPointController` 등)
* **`Data/`:** `AppDbContext` (DB 컨텍스트), `IUserSession` (현재 로그인 스트리머 세션)
* **`Models/`:** EF Core 엔터티 (`StreamerProfile`, `ViewerProfile` 등)
* **`Features/`:** MediatR 핸들러 모음. 비즈니스 로직의 핵심 계층. (예: `ViewerPointEventHandler`)
* **`Services/`:** 백그라운드 워커 (`ChzzkBackgroundService`) 및 공유 유틸리티.
* **`Hubs/`:** SignalR 실시간 통신 (`OverlayHub`).

## 3. ⚠️ 전역 기술 규칙 및 주의사항 (Global Rules)
코드를 작성할 때 아래의 프로젝트 전역 규칙을 반드시 준수하십시오.

1. **치지직 WebSocket 프로토콜 수동 파싱:** `SocketIOClient` 라이브러리를 사용하지 않습니다. `ClientWebSocket`을 사용하여 텍스트를 `Substring(2)`로 자른 뒤 이중 `JsonDocument.Parse`를 수행하여 이벤트를 파싱해야 합니다.
2. **멀티테넌트 API 보안:** 모든 Controller 엔드포인트는 `[Authorize]` 속성을 통해 보호되어야 하며, `IUserSession.ChzzkUid`와 URL의 식별자가 일치하는지 검증하여 타 스트리머의 데이터 접근을 차단해야 합니다.
3. **HTTP Client 인스턴스 과부하 방지:** `new HttpClient()`를 직접 생성하지 마십시오. 반드시 DI 컨테이너에 등록된 `IHttpClientFactory`를 통해 클라이언트를 주입받아야 합니다.
4. **봇 토큰 폴백(Fallback) 우선순위:** 치지직 API 호출 시 1순위 `StreamerProfile.BotAccessToken`, 2순위 `SystemSettings` 전역 봇 토큰, 3순위 스트리머 본인 `ChzzkAccessToken` 순으로 사용합니다.