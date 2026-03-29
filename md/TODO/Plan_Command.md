# 명령어 시스템 문제 해결 계획 (Plan_Command)

작성일: 2026-03-29
대상: MooldangBot 통합 명령어 시스템 아키텍처 개선안 (`Research_Command.md` 후속 조치)

## 1. 상태 불일치 및 예외 롤백 부재 해결 방안

**현행 문제**: 재화(치즈/포인트)가 검증 단계에서 먼저 소모되고, 이후 실행되는 `ICommandFeatureStrategy` 로직이 실패(API 오류 등)할 경우 포인트가 증발하는 문제 발생.

**해결안 (Action Plan)**:
*   **보상 트랜잭션(Compensating Transaction) 패턴 도입**:
    *   `ICommandFeatureStrategy.ExecuteAsync`의 반환 타입을 `Task<CommandExecutionResult>` 구조체(성공 여부, 에러 메시지 포함)로 변경합니다.
    *   `UnifiedCommandHandler`는 전략 실행 결과를 대기하고, 결과가 실패(`IsSuccess == false`)로 반환될 경우 즉시 차감했던 사용자 재화를 복구(Refund)하는 후속 로직을 실행합니다.
*   **사용자 경험(UX) 개선**:
    *   에러 발생 및 환불 시 사일런트 실패를 피하고, "⚠️ 시스템 오류로 인해 차감된 {비용}P가 환불되었습니다." 형태의 시스템 메시지를 채팅으로 전송하여 신뢰성을 확보합니다.

## 2. 동시성(Concurrency) 및 스레드 경합 해결 방안

**현행 문제**: 짧은 시간에 다수의 시청자가 동시에 시스템 명령어를 수정(`SystemResponseStrategy`)하거나 포인트를 깎는 룰렛을 돌릴 경우, EF Core의 Read-Modify-Write 단계를 밟아 트랜잭션 오버랩 및 정보 소실(Lost Update) 발생 가능.

**해결안 (Action Plan)**:
*   **재화 차감 시 원자적(Atomic) 증가/감소 쿼리 적용**:
    *   .NET의 향상된 LINQ인 `ExecuteUpdateAsync()`를 사용하여 DB 락 레벨에서 원자적 처리를 수행합니다.
    *   코드 예시:
        ```csharp
        int updatedRows = await db.ViewerProfiles
            .Where(v => v.StreamerChzzkUid == profile.Uid && v.ViewerUid == senderId && v.Points >= cost)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Points, p => p.Points - cost), ct);
        if (updatedRows == 0) return false; // 잔여 포인트 부족
        ```
*   **명령어 정보 (ResponseText) 수정 시 낙관적 동시성 제어**:
    *   한 행의 데이터가 빈번히 수정되는 `UnifiedCommand` 엔티티 업데이트 시, Entity Framework의 `[ConcurrencyCheck]` 또는 `RowVersion`을 적용하여 `DbUpdateConcurrencyException` 예외를 잡은 뒤 우아하게 재시도(Retry)하거나 앞선 요청을 우선 처리하도록 제어합니다.

## 3. Hardcoded 문자열 의존성 (Type-Safety 저하) 해결 방안

**현행 문제**: 전략 분기를 담당하는 `FeatureType`이 `string` 데이터 타입(`"Notice"`, `"Reply"`, `"Title"`)에 직접 의존하여, 오타 및 리팩토링 시 런타임 버그의 원인이 됨.

**해결안 (Action Plan)**:
*   **강타입 상수(Strongly-Typed Constants) 객체화**:
    *   MariaDB 테이블의 스키마 변경 비용과 EF Core의 Enum 매핑 오버헤드를 줄이면서 Type-Safety를 지키는 가장 좋은 방법은, 핵심 정적 구조체를 만들어 관리하는 것입니다.
    *   ```csharp
        public static class CommandFeatureTypes 
        {
            public const string Reply = "Reply";
            public const string Notice = "Notice";
            public const string Title = "Title";
            public const string Category = "Category";
            // ...
        }
        ```
*   도메인 모델(`UnifiedCommand.FeatureType`)과 전략 모델(`ICommandFeatureStrategy.FeatureType`) 양측 모두 리터럴 문자열 대신 이 구조체를 참조하도록 강제(Enforce)하여 컴파일 레벨에서 오타를 차단합니다.

## 4. API Rate Limiting 체인 딜레이 해결 방안

**현행 문제**: 치지직 API 응답 지연 시, MediatR In-Process 워커 스레드가 블로킹되어 다른 채팅 이벤트들의 파이프라인 처리가 연쇄적으로 늦어짐(Delay Chain).

**해결안 (Action Plan)**:
*   **Polly 기반의 탄력성(Resilience) 정책 도입**:
    *   .NET 10에서 기본으로 지원하는 향상된 `Microsoft.Extensions.Http.Resilience` 패키지를 도입합니다.
    *   외부 API (방제 변경, 카테고리 검색 등) 클라이언트에 의도적인 Timeout (예: 2초) 및 Circuit Breaker 패턴을 부착합니다. 이를 통해 지연이 임계치를 넘으면 빠른 실패(Fail-Fast) 시키고, 곧바로 보상 트랜잭션으로 빠지게 만듭니다.
*   **Task 비동기 분리 처리 (Fire & Forget 또는 Channel 큐잉)**:
    *   응답이 지연될 우려가 있는 대규모 로직 연산이 필요한 부분(예: 다중 룰렛 등)은 메인 `Handle` 파이프라인에서 Task를 분리합니다.
    *   .NET의 `System.Threading.Channels.Channel<T>` 기반 백그라운드 큐로 오프로딩(Off-loading)하여, 메인 채팅 핸들러는 0.1초 내외로 "접수" 응답만 내뱉고 종료되며, 실제 처리는 별도의 소비자(Consumer) 백그라운드 스레드가 비동기로 차례대로 처리하도록 구조를 재설계합니다.
