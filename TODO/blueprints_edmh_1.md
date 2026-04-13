# 🔱 [오시리스의 전령]: MassTransit 인프라 최종 집도 계획 (EDMH)

본 계획은 지휘관님(mooldang)의 최종 교정안을 반영하여, 어셈블리 스캐닝 함정을 피한 완성된 MassTransit 인프라를 구축하고 기존의 `IRabbitMqService`를 제거하여 시스템을 순수 EDMH 아키텍처로 전환하는 것을 목표로 합니다.

## User Review Required

> [!IMPORTANT]
> **전환 순서 결정**: 인프라 구축 직후 **발행자(Publisher)** 측의 컴파일 에러를 우선 해결하여 시스템의 '송신 혈관'을 먼저 개통하겠습니다. 이후 소비자를 순차적으로 전환합니다.

> [!CAUTION]
> **컴파일 에러 감수**: `IRabbitMqService` 및 `RabbitMQPersistentConnection` 제거 시 솔루션 전역에 일시적으로 많은 컴파일 에러가 발생합니다. 이는 계획된 과정이며, 단계적으로 모든 참조를 `IPublishEndpoint` 및 `IConsumer<T>`로 교체하겠습니다.

---

## Proposed Changes

### 1. [Infrastructure] 인프라 최종 구성

#### [MODIFY] [DependencyInjection.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/DependencyInjection.cs)
- 지휘관님의 **'Assembly Scanning 교정안'**을 전격 수용하여 `AddMessagingInfrastructure` 확장 메서드 구현.
- 기존의 레거시 RabbitMQ 관련 서비스(162~186라인) 완전 삭제.
- 환경 변수(`RABBITMQ_HOST`, `RABBITMQ_PORT` 등) 바인딩 및 서킷 브레이커 설정 완료.

---

### 2. [Phase 1] 발행자(Publisher) 전환 (컴파일 에러 해결)

#### [DELETE] [IRabbitMqService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Interfaces/IRabbitMqService.cs)
#### [MODIFY] [UnifiedCommandHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Features/Commands/Handlers/UnifiedCommandHandler.cs)
- `IRabbitMqService` 의존성을 제거하고 `IPublishEndpoint`로 교체.
- `PublishAsync` 호출을 MassTransit의 타입 기반 `Publish`로 전환.

---

### 3. [Phase 2] 소비자(Consumer) 전환 및 레거시 제거

#### [NEW] `ChatReceivedConsumer.cs`
- `IConsumer<ChatReceivedEvent>` 구현체 작성.
#### [DELETE] [ChzzkEventRabbitMqConsumer.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/ChzzkEventRabbitMqConsumer.cs)
- 수동 구독 로직 폐기.

---

## Verification Plan

### Automated Tests
- `dotnet build` 수행: 모든 프로젝트의 컴파일 성공 여부 확인.
- `Verifier` 도구 기동: 신규 토폴로지에서의 데이터 직렬화 무결성 진단.

### Manual Verification
- RabbitMQ Management UI: 네임스페이스 기반의 자동 생성된 익스체인지 구조 확인.
- 로그 분석: `MassTransit`의 연결 성공 및 메시지 수신 로그 확인.
