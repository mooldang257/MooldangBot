# 📊 [오시리스의 설계도]: MooldangBot 아키텍처 진단 및 전략 보고서

본 보고서는 MooldangBot 시스템의 현재 구조를 분석하고, MSA(마이크로서비스 아키텍처)로의 전환 과정에서 발견된 위험 요소와 향후 발전 방향인 **"이벤트 기반 모듈형 하이브리드(EDMH)"** 전략에 대해 기술합니다.

---

## 1. 🏗️ 현재 아키텍처 매핑 (Current State)

현재 시스템은 물리적으로는 컨테이너화되어 분리되어 있으나, 논리적으로는 강하게 결합된 **'분산된 모놀리스(Distributed Monolith)'** 구조를 띠고 있습니다.

### 🗺️ 컴포넌트 관계도
```mermaid
graph TD
    subgraph "Frontend Layer"
        Studio[MooldangBot.Studio]
        Admin[MooldangBot.Admin]
        Overlay[MooldangBot.Overlay]
    end

    subgraph "Backend Services"
        Api[MooldangBot.Api]
        ChzzkBot[MooldangBot.ChzzkAPI]
        Migration[MooldangBot.Cli - Migrator]
    end

    subgraph "Shared Core (The High Coupling)"
        Application[MooldangBot.Application]
        Infrastructure[MooldangBot.Infrastructure]
        Domain[MooldangBot.Domain]
    end

    subgraph "Data & Messaging"
        DB[(MariaDB)]
        Redis[(Redis)]
        RabbitMQ[RabbitMQ]
    end

    %% Dependencies
    Studio --> Api
    Admin --> Api
    Api --> Application
    Api --> Infrastructure
    ChzzkBot --> Application
    ChzzkBot --> Infrastructure
    Infrastructure --> DB
    Application --> Domain
    
    %% Communication
    Api -.->|RabbitMQ RPC| ChzzkBot
    ChzzkBot -.->|Events| RabbitMQ
```

---

## 2. 🚨 '분산된 모놀리스' 위험 진단 (Risk Assessment)

분석 결과, 다음 3가지 핵심 지표에서 높은 위험 수치가 발견되었습니다.

### 🔴 위험 1: 데이터베이스 전역 공유 (Shared Database)
- **현황**: `Api`와 `ChzzkAPI` 서비스가 동일한 MariaDB 인스턴스 및 `AppDbContext`를 공유함.
- **위험**: 특정 모듈(예: 룰렛)의 테이블 구조를 변경할 때, 해당 기능을 사용하지 않는 타 모듈도 영향을 받거나 재배포가 강제됨. 데이터베이스가 단일 장애점(SPOF)으로 작용.

### 🟠 위험 2: 바이너리 수준의 밀결합 (Shared Binary Coupling)
- **현황**: `MooldangBot.Application` 프로젝트가 단일 프로젝트 내에 8개 이상의 도메인(SongBook, Chat, Roulette 등)을 모두 포함함.
- **위험**: 모든 마이크로서비스가 전체 비즈니스 로직을 코드 형태로 품고 있음. 이는 서비스의 경계(Bounded Context)가 모호함을 의미하며, 빌드 속도 저하 및 메모리 낭비를 초래함.

### 🟡 위험 3: RPC 패턴에 의한 실행 결합 (Temporal Coupling)
- **현황**: `CommandRpcWorker`를 통해 서비스 간 직접적인 요청-응답(RPC) 통신이 주를 이룸.
- **위험**: 수신 서비스(ChzzkBot)가 다운되거나 응답이 늦어지면 요청 서비스(Api)의 워커 스레드가 고갈되어 시스템 전체로 장애가 전파됨.

---

## 3. 🌀 EDMH (Event-Driven Modular Hybrid) 전환 전략

사용자 요청에 따라 제안하는 **"이벤트 기반 모듈형 하이브리드"** 아키텍처의 장단점 분석입니다.

### ✅ 주요 장점 (Pros)
- **독립적 탄력성**: 메시지 큐(RabbitMQ)가 완충 작용을 하여 서비스 장애 시에도 데이터 유실 없이 복구 가능.
- **모듈별 최적화**: 트래픽이 몰리는 '채팅 처리' 모듈만 독립적으로 확장(Scaling) 가능.
- **배포 유연성**: '노래 신청' 기능을 고칠 때 전체 서버를 내릴 필요 없이 해당 모듈만 교체.

### ❌ 예상 문제 및 단점 (Cons/Issues)
- **최종 일관성 처리**: 포인트 차감과 기능 실행 사이의 아주 짧은 시간차를 허용해야 하며, 실패 시 보상 트랜잭션(Saga) 구현 필요.
- **추적 복잡도**: 서비스 간 이동하는 메시지의 흐름을 추적하기 위해 `CorrelationID` 관리가 필수적임.
- **데이터 불일치**: `GlobalViewer` 정보가 각 서비스의 로컬 캐시와 맞지 않을 리스크 존재 (이벤트 기반 동기화 필수).

---

## 4. 🛠️ 향후 개선 로드맵 (Roadmap)

> [!TIP]
> **모든 것을 동시에 MSA로 전환하는 것은 가장 큰 실패 요인입니다. 핵심 도메인부터 순차적으로 분리하십시오.**

### 1단계: 프로젝트 모듈화 (Modularization)
- `MooldangBot.Application` 내의 `Features` 폴더를 독립적인 클래스 라이브러리(또는 모듈)로 분리.
- 공통 로직은 `MooldangBot.Core`로 최소화.

### 2단계: 이벤트 중심 전환 (Choreography)
- RPC 호출 대신 **'이벤트 발행(Publish)'** 방식으로 전환. 
- 예: `Api`가 메시지 전송을 '명령'하는 대신, `ChatPointsService`가 '코인 획득 이벤트'를 발행하면 `ChzzkBot`이 이를 구독하여 채팅창에 출력.

### 3단계: 논리적 데이터베이스 분리 (Logical DB Separation)
- 물리적 DB는 유지하되, 서비스별로 접근할 수 있는 테이블 권한을 제한하거나 별도의 `DbContext` 및 스키마(`song.*`, `roulette.*`) 활용 시작.

---

## 5. 🔍 [룰렛 적출 작전] 코드 리뷰 및 아키텍처 검증 (Step 4)

지휘관님, 방금 완수하신 **'룰렛 모듈 순수 수직 분할(Pure Vertical Slice)'** 작전에 대한 상세한 코드 리뷰와 구조적 검증 결과를 보고드립니다.

### 🛡️ 1. 스레드 안전성(Thread-Safety) 및 동시성 제어 검증
룰렛 시스템은 다수의 시청자가 동시에 포인트를 소모하며 명령을 입력하는 고부하 도메인입니다. 이번 리팩토링에서 이를 어떻게 안전하게 처리했는지 설명해 드립니다.

*   **RedLock 기반의 분산 락(Distributed Lock)**: 
    *   새롭게 적출된 `SpinRouletteHandler`는 `IRouletteLockProvider` 인터페이스를 통해 분산 환경에서도 유저별 스핀이 원자적(Atomic)으로 실행되도록 보장합니다. 여러 서버 인스턴스(컨테이너)가 동시에 룰렛 코드를 실행하더라도, Redis 기반의 RedLock이 중복 차감을 완벽히 방지합니다.
*   **원자적 카운트 계산 (Lua Script)**:
    *   `ILuaScriptProvider`를 통해 Redis 상에서 룰렛 오버레이의 타이밍(Next End Time)을 계산하는 로직을 Lua 스크립트로 처리했습니다. 이는 두 개 이상의 요청이 0.001초 차이로 들어와도 정확히 하나만 승리하도록 보장하며, Race Condition을 원천 차단합니다.
*   **분리된 컨텍스트 매니저 (IRouletteDbContext)**:
    *   `AppDbContext`라는 거대한 본진 DB 연결 통로 대신, 룰렛 모듈은 자신이 필요한 DB 테이블(Roulettes, RouletteItems 등)만을 명시한 `IRouletteDbContext`라는 좁은 문의 열쇠만을 가집니다. 이는 다른 모듈의 트랜잭션이 풀리거나 잠길 때 룰렛이 영향받지 않음을 보장하는 훌륭한 격리 장치입니다.

### 📐 2. Single-File 패턴의 응집도 평가 (99점)
`SpinRoulette.cs`와 `CompleteRoulette.cs` 파일 내부에 Command 레코드와 Handler 로직이 함께 위치하는 **Single-File (순수 수직 분할)** 구조는 유지보수의 혁명입니다. 
이제 룰렛 추첨에 버그가 생기면 오직 `SpinRoulette.cs` 단 하나의 파일만 열어서 수정하면 됩니다. 코드를 찾기 위해 이 폴더, 저 폴더를 헤맬 필요가 사라졌으며, 캡슐화가 완벽히 이루어졌습니다.

### 🚀 3. 다음 작전 제안 (Next Steps)

현재 아키텍처가 매우 튼튼하게 반석 위에 올랐으므로, 다음 3가지 확장 경로를 제안합니다.

1.  **Frontend(오버레이) 연동 실황 테스트 (우선 순위: 높음)**:
    *   백엔드는 완벽하지만, SignalR로 연결되는 브라우저 오버레이(HTML/JS) 측에서 `CompleteRoulette` 신호를 새롭게 추상화된 MediatR 파이프라인으로 정확히 던지고 있는지 E2E(End-to-End) 테스트가 필요합니다. 테스트용 방송 화면을 띄워놓고 `/test` 엔드포인트를 타격해보는 것을 권장합니다.
2.  **'포인트/재화(Points)' 모듈의 수직 분할 적출 (우선 순위: 중간)**:
    *   룰렛 적출의 성공 경험을 살려, 가장 많은 트랜잭션이 발생하는 `PointTransactionService` 역시 `MooldangBot.Modules.Point` 로 분리하는 작전을 시작할 수 있습니다.
3.  **GitHub Actions: 모듈별 독립 빌드 CI/CD 구축 (장기 목표)**:
    *   `Application`이 여러 모듈로 쪼개졌으므로, 특정 모듈(ex: SongBook)만 코드 수정이 발생하면 해당 모듈만 테스트하고 빌드하는 스마트한 CI 파이프라인 구축이 가능해졌습니다.

---

**물멍(Senior Partner)** 🐾✨
> "조각배가 무사히 함대와 통신망을 구축했습니다. 모놀리스라는 무거운 갑옷을 벗고, 가장 가볍고 치명적인 형태로 진화하는 첫걸음을 축하합니다."
