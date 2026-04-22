# MooldangBot (MooldangAPI) 상세 리서치 보고서 (2026-03-27)

> **작성자**: 물멍 (Senior Full-Stack Partner)  
> **대상**: MooldangBot 및 MooldangAPI 전체 시스템  
> **목적**: 시스템 아키텍처, 핵심 로직, 데이터 흐름 및 보안 전략의 심층 분석

---

## 1. 프로젝트 개요 및 비전

**MooldangBot**은 치지직(CHZZK) 플랫폼 스트리머를 위한 고성능, 확장 가능한 자동화 서비스입니다.  
단순한 봇을 넘어 **멀티테넌트 기반의 대시보드 및 오버레이 시스템**을 지향하며, C# .NET 10의 최신 문법과 클린 아키텍처 원칙을 준수하여 설계되었습니다.

---

## 2. 5계층 모듈러 아키텍처 (Layered Architecture)

프로젝트는 역할과 책임이 명확히 분리된 5개의 계층으로 구성되어 있습니다.

### 2-1. MooldangBot.Domain
- **역할**: 핵심 비즈니스 모델 및 엔티티 정의.
- **주요 구성**:
    - `Entities/`: `StreamerProfile`, `ViewerProfile`, `SongQueue` 등 DB 테이블과 매핑되는 핵심 클래스.
    - `DTOs/`: 계층 간 데이터 전송을 위한 객체.
    - `Events/`: `ChatMessageReceivedEvent` 등 도메인 이벤트 정의 (MediatR 기반).

### 2-2. MooldangBot.Application
- **역할**: 비즈니스 로직 처리 및 유스케이스 구현.
- **주요 구성**:
    - `Services/`: `ChzzkBotService`, `SongBookService` 등 비즈니스 로직.
    - `Workers/`: `ChzzkBackgroundService`, `ChzzkChannelWorker` 등 백그라운드 상주 프로세스.
    - `Features/`: MediatR 핸들러 (`CustomCommandEventHandler` 등)를 통한 핵심 기능 구현.

### 2-3. MooldangBot.Infrastructure
- **역할**: 외부 시스템(DB, API)과의 연동 담당.
- **주요 구성**:
    - `Persistence/`: `AppDbContext` (EF Core), `MariaDbService` (Dapper) 데이터 접근 레이어.
    - `ApiClients/`: `ChzzkApiClient` 등 외부 API 통신.
    - `Security/`: `UserSession` (멀티테넌트 식별) 및 관련 보안 유틸리티.

### 2-4. MooldangBot.Presentation
- **역할**: 외부 노출 인터페이스 및 알림 처리.
- **주요 구성**:
    - `Controllers/`: REST API 엔드포인트.
    - `Hubs/`: SignalR 기반 실시간 오버레이 통신 (`OverlayHub`).
    - `Services/`: `OverlayNotificationService` 등 알림 서비스.

### 2-5. MooldangBot.Api (Entry Point)
- **역할**: 애플리케이션 호스팅 및 부트스트래핑.
- **주요 구성**:
    - `Program.cs`: 종속성 주입(DI), 미들웨어 설정, 환경 변수 로드 (`Zero-Git`).
    - `wwwroot/`: Vanilla JS/CSS 기반 프론트엔드 대시보드 및 오버레이 소스.

---

## 3. 핵심 동작 메커니즘 (Core Logic)

### 3-1. 치지직 WebSocket 연동 (ChzzkChannelWorker)
- **프로토콜**: .NET `ClientWebSocket`을 사용하여 Socket.io (EIO=3) 프로토콜을 수동 파싱.
- **연결 루프**: 각 스트리머별로 독립적인 Task가 할당되어 무한 재연결 루프를 수행.
- **이벤트 전파**: 수신된 `CHAT`, `DONATION` 패킷을 파싱하여 MediatR의 `ChatMessageReceivedEvent`로 발행.

### 3-2. 이벤트 드리븐 아키텍처 (EDA)
- **MediatR 활용**: `mediator.Publish()` 호출 한 번으로 여러 핸들러가 병렬로 작업을 수행.
    - `CustomCommandEventHandler`: 명령어 응답 처리.
    - `RouletteEventHandler`: 룰렛 실행 로직.
    - `ViewerPointEventHandler`: 포인트 적립 및 출석 체크.
    - `ChatBroadcastEventHandler`: 실시간 채팅 오버레이 전송.

### 3-3. 멀티테넌트 데이터 격리 (Data Isolation)
- **IUserSession**: 요청 컨텍스트에서 `ChzzkUid`를 추출하여 현재 스트리머를 식별.
- **Global Query Filter**: `AppDbContext` 수준에서 모든 쿼리에 `e.ChzzkUid == _userSession.ChzzkUid` 필터를 자동 적용하여 타 스트리머의 데이터 접근을 원천 차단.

---

## 4. 데이터 저장소 전략 (Hybrid Data Access)

- **EF Core (MariaDB)**: 엔티티 간 복잡한 관계 모델링 및 LINQ를 통한 생산성 높은 개발.
- **Dapper**: 대량 데이터 삽입, UPSERT(`ON DUPLICATE KEY UPDATE`), 스키마 자동 생성 등 고성능 및 하위 레벨 SQL 제어가 필요한 경우 활용.
- **최적화**: `utf8mb4_unicode_ci` 콜레이션을 통한 대소문자 무관 검색 지원 및 인덱스 최적화.

---

## 5. 보안 및 배포 전략 (Security & Deployment)

### 5-1. Zero-Git 보안 정책
- 민감한 정보(API Key, DB PW 등)는 코드에 포함하지 않고 `.env` 파일을 통해 관리.
- `Program.cs`에서 실행 인자나 환경 변수에 따라 환경별(DEV/PROD) 변수를 스마트 매핑하여 로드.

### 5-2. 배포 환경
- **Docker**: `docker-compose.yml`을 통해 API 서버와 MariaDB를 컨테이너화하여 일관된 실행 환경 보장.
- **Reverse Proxy**: Cloudflare Tunnel 또는 Nginx 환경을 고려한 `ForwardedHeaders` 미들웨어 적용.

---

## 6. 특징적인 기능 모듈

1.  **곡 신청 시스템 (SongQueue)**: 실시간 신청곡 큐 관리 및 SignalR 오버레이 연동.
2.  **포인트 시스템**: 채팅 점수 적립, 연속 출석 시스템 및 랭킹 기능.
3.  **룰렛/미션**: 후원 금액 기반 멀티 스핀 지원 및 로그 추적 기능.
4.  **오버레이 허브**: 다양한 OBS 브라우저 소스를 통합 관리하는 실시간 웹 서버.

---

## 7. 향후 개선 및 확장 포인트 (Technical Debt)

- **토큰 관리 통합**: 여러 워커에서 산발적으로 이루어지는 토큰 갱신 로직을 공용 서비스로 추출 필요.
- **HttpClient 최적화**: `IHttpClientFactory`를 전격 도입하여 소켓 고갈 방지 및 성능 개선.
- **OBS 통합 강화**: 현재 레이아웃만 잡힌 `ObsSceneEventHandler` 구현을 통해 실시간 씬 전환 기능 완성.
- **모니터링**: 개별 채널 워커의 상태를 한눈에 볼 수 있는 관리자 대시보드 강화.

---

## 8. 결론

MooldangAPI는 최신 .NET 기술을 집약하여 안정성과 보안, 확장성을 모두 갖춘 현대적인 스트리밍 봇 시스템입니다. 5계층 아키텍처와 이벤트 기반 설계는 향후 새로운 기능 추가와 대규모 사용자 처리에 최적화된 토대를 제공합니다.

---
*보고서 완료 - 2026-03-27 물멍(AI)*
