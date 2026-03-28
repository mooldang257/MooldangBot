# MooldangBot 통합 명령어 아키텍처 연구 보고서

작성일: 2026-03-29
대상: MooldangBot 통합 명령어 시스템 (`UnifiedCommandHandler`, `ICommandFeatureStrategy` 등)

---

## 1. 명령어의 카테고리 및 구조 
현재 MooldangBot의 명령어 시스템은 고도화된 **전략 패턴(Strategy Pattern)** 기반의 라우팅 구조를 가지고 있습니다.

- **도메인 모델 (Entities & Enums)**
  - `CommandCategory` Enum을 통해 크게 3가지로 분류됩니다.
    - `General` (일반)
    - `System` (시스템 메시지/공지)
    - `Feature` (기능 호출)
  - 엔티티인 `UnifiedCommand`는 `ChzzkUid`와 `Keyword`의 조합으로 고유성을 보장합니다.
  - 명령어마다 `CostType`(치즈, 포인트, 없음)과 `RequiredRole`(Viewer, Manager, Streamer) 기반의 실행 조건을 정밀하게 제어할 수 있습니다.
  - 실제 어떤 동작을 할지는 `FeatureType` 문자열(예: `"Reply"`, `"Notice"`, `"Title"`, `"Category"`)에 의해 결정됩니다.

- **계층형 처리 로직 (UnifiedCommandHandler)**
  - 채팅 이벤트가 발생하면 단일 진입점인 통합 핸들러가 가동됩니다.
  - 캐시 조회 -> 권한/재화 검증(`ValidateRequirementAsync`) -> 해당하는 `ICommandFeatureStrategy` 검색 및 실행(`ExecuteAsync`) 순으로 처리됩니다.

---

## 2. 기능이 추가될 경우 발생할 수 있는 문제점

1. **상태 불일치 및 예외 롤백 부재**
   - 현재 핸들러(`UnifiedCommandHandler`)에서 명령어 비용(포인트/치즈) 차감이 선행된 후, 하위 전략(`Strategy`)이 실행되는 구조입니다. 
   - 만약 하위 전략(예: 방제 변경 실패, 치지직 API 에러)에서 실행 중 오류가 발생할 경우, 이미 차감된 시청자의 재화(포인트)를 되돌려주는 **보상 트랜잭션 혹은 롤백 메커니즘**이 구현되어 있지 않아 CS 이슈가 발생할 수 있습니다.

2. **동시성(Concurrency) 및 스레드 경합**
   - 시스템 명령어 수정(`SystemResponseStrategy`)이나 룰렛 다중 전송 등 DB의 Entity 수정이 필요한 작업을 많은 시청자가 동시에 호출할 경우, `SaveChangesAsync()` 단계에서 트랜잭션 경합이 일어날 여지가 있습니다.
   
3. **Hardcoded 문자열 의존성 (Type-Safety 저하)**
   - 각 전략의 식별자가 `FeatureType`이라는 `String` 기반으로 라우팅됩니다. 타입 오타 시나리오를 컴파일 타임에 잡기 힘들어 런타임에 처리되지 않는 버그가 발생할 수 있습니다.

4. **API Rate Limiting 체인 딜레이**
   - 외부 API 연동 기능이 추가될 경우 응답 지연이 발생할 수 있는데, 예외 처리나 타임아웃 관리가 부실하면 MediatR 파이프라인 전체에 지연을 초래해 봇 반응 속도가 느려질 수 있습니다.

---

## 3. 확장성 (Scalability)

현재 아키텍처의 **확장성은 매우 우수**합니다. 
SOLID 원칙 중 **OCP(개방-폐쇄 원칙)** 를 철저히 준수하도록 설계되어 있습니다.

- **독립적인 처리 단위**
  - 새로운 명령어를 등록하거나 기능을 추가할 때 기존 코드(`UnifiedCommandHandler` 등)를 수정할 필요가 없습니다. 모듈 간 결합도가 크게 낮아져 있습니다.
- **자동 주입(DI)**
  - `IEnumerable<ICommandFeatureStrategy>`를 활용하여 전략들을 한 번에 주입받고, `.FirstOrDefault(s => s.FeatureType == ...)`를 통해 동적 라우팅을 수행하므로, 구현체만 만들면 즉시 시스템에 편입됩니다.
- **관심사의 분리 (Separation of Concerns)**
  - 공통 로직(재화 소모, 권한 체크, 명령 파싱)은 Handler가 담당하고, 각 비즈니스 로직만 별도 클래스가 담당하여 코드가 깔끔하게 유지됩니다.

---

## 4. 확장 시 주의할 점

1. **DB Context 스코프(Scope) 격리 (스레드 안전성 확보)**
   - 비동기 채팅 처리 상황(다중 스레드)에서 싱글톤 또는 라이프사이클이 긴 인스턴스의 DbContext를 공유하면 예외가 발생합니다.
   - 전략 내에서 DB 작업이 필요하다면 기존 전략들처럼 **반드시 `IServiceProvider.CreateScope()`를 이용해 1회용 DB Context 스코프를 생성하여 사용(Thread-safe)**해야 합니다.
2. **독립적인 에러 핸들링 구성**
   - `ExecuteAsync` 내부에서 발생하는 예외가 throw되어 MediatR 파이프라인 상위로 올라가지 않도록, 각 전략 안에서 철저히 `try-catch`로 감싸고 사용자에게 알림 메시지(`SendReplyChatAsync`)를 전송하여 프로세스 다운을 막아야 합니다.
3. **유효성 검증의 책임 구분**
   - 재화/권한과 같은 공통 검증은 통합 핸들러 로직을 따르고, 그 외 비즈니스 규칙(예: 명령어 인풋 길이 초과 등)만 각 전략의 첫 단에서 방어 코드로 차단하도록 책임을 명확히 구분해야 합니다.

---

## 5. 확장하는 방법 (Feature 추가 워크플로우)

새로운 명령어 기능(예: 날씨 조회 기능)을 확장하려면 다음 절차를 따릅니다.

1. **전략 구현체(Strategy) 생성**
   - `MooldangBot.Application.Features.Commands.Strategies` 네임스페이스 하위에 `ICommandFeatureStrategy` 인터페이스를 구현하는 새로운 클래스(예: `WeatherStrategy.cs`)를 만듭니다.
2. **FeatureType 정의 및 로직 구현**
   - 해당 객체의 `FeatureType` 프로퍼티 선언 시, 고유한 문자열 레이블(예: `"Weather"`)을 리턴하도록 지정합니다.
   - `ExecuteAsync` 메서드 안에 외부 API/DB 연동 및 봇 채팅 응답 로직을 구현합니다.
3. **DI 관련 서비스 주입 처리**
   - 생성자(`Primary Constructor`)에 필요한 서비스 모듈(IChzzkBotService, IServiceProvider 등)만 선언합니다. 프레임워크가 기동 시 자동으로 `IEnumerable`에 수집해 반영해 줍니다.
4. **마스터 데이터 추가**
   - DB의 `UnifiedCommand` 테이블(또는 프론트엔드 관리자 UI)에 새 명령어를 추가하고 `FeatureType` 값을 새로 구현된 값(`"Weather"`)으로 입력합니다.
5. **적용 및 테스트**
   - 런타임에서 사용자가 해당 키워드를 입력하면 검증 프로세스를 거친 뒤 개발된 `WeatherStrategy`로 자동 분기되어 실행됩니다.
