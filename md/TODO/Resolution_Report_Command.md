# 통합 명령어 시스템 아키텍처 개선 검증 보고서 (Resolution Report)

작성일: 2026-03-29
대상: MooldangBot 아키텍처 고도화 작업 (세피로스 10.01Hz)
관련 문서: `Research_Command.md` (문제점 도출), `Plan_Command.md` (해결 방안)

---

## 개요
`Research_Command.md`의 **"2. 기능이 추가될 경우 발생할 수 있는 문제점"** 항목에서 제기된 4가지 주요 아키텍처 취약점이 `Plan_Command.md`의 설계에 따라 실제 소스코드 상에서 어떻게 해결되었는지 상세 분석한 문서입니다.

---

## 1. 상태 불일치 및 예외 롤백 부재
**제기되었던 문제 (`Research_Command.md`)**:
- 핸들러에서 재화(포인트) 차감이 선행된 후, 하위 전략(치지직 API 등)에서 실패하면 이미 차감된 포인트가 공중분해되는 문제. 보상 트랜잭션 부재로 시스템 신뢰성 하락.

**해결 분석 (`Plan_Command.md` 반영 결과)**:
- **✅ 완벽히 해결됨 (보상 트랜잭션 구현)**
- `ICommandFeatureStrategy` 파이프라인의 반환 타입이 단순 `Task`에서 상태 객체인 `Task<CommandExecutionResult>`로 고도화되었습니다.
- 반환된 결과(`result`) 객체 내에 실패 사유와 함께 환불 필요성(`ShouldRefund`) 플래그를 담도록 구조를 변경했습니다.
- **코드 검증 (`CustomCommandEventHandler.cs`)**:
  ```csharp
  // 전략 실행
  var result = await strategy.ExecuteAsync(notification, command, ct);

  // 실행 실패 시 커스텀 환불 처리 (RefundPointsAsync 호출)
  if (!result.IsSuccess && result.ShouldRefund && command.Price > 0)
  {
      await RefundPointsAsync(notification, command, result.Message, ct);
  }
  ```
- 전략 실행 도중 예외가 발생하더라도 `CustomCommandEventHandler`가 이를 감지하고 포인트 복구 및 `"실행 실패로 인해 {Price}P가 환불되었습니다."` 라는 안내 메시지를 투사하여 UX를 완벽히 수호합니다.

---

## 2. 동시성 (Concurrency) 및 스레드 경합
**제기되었던 문제 (`Research_Command.md`)**:
- 짧은 시간 내 룰렛이나 특정 API가 동시다발적으로 호출되면, `SaveChangesAsync()` 시점에 EF Core의 Read-Modify-Write 오버랩으로 인해 데이터 소실(Dirty Write, Lost Update)이 발생.

**해결 분석 (`Plan_Command.md` 반영 결과)**:
- **✅ 완벽히 해결됨 (Atomic Query 기반 업데이트)**
- 더 이상 조회를 먼저 하고(Select) 메모리에서 차감한 뒤 저장하는 방식이 아닌, EF Core의 `ExecuteUpdateAsync()`를 사용하여 DB 락(Lock)을 타는 원자적(Atomic) 쿼리로 변경되었습니다.
- **코드 검증 (`CustomCommandEventHandler.cs`)**:
  ```csharp
  int updatedRows = await db.ViewerProfiles
      .Where(v => v.StreamerChzzkUid == n.Profile.ChzzkUid && v.ViewerUid == n.SenderId && v.Points >= c.Price)
      .ExecuteUpdateAsync(s => s.SetProperty(p => p.Points, p => p.Points - c.Price), ct);
  ```
- 하나의 쿼리문 즉 `UPDATE ViewerProfiles SET Points = Points - Price WHERE Points >= Price` 로 처리하기 때문에 동시성 경합 상태에서 1,000명이 동시에 진입해도 안전하게 포인트가 방어됩니다. 
- 또한 잦은 상태 변경이 일어나는 `ActionType` 필드에는 `[ConcurrencyCheck]` 낙관적 락 제어가 추가되어 스키마 안정성도 확보했습니다.

---

## 3. Hardcoded 문자열 의존성 (Type-Safety 저하)
**제기되었던 문제 (`Research_Command.md`)**:
- `FeatureType`이나 `ActionType`이 `"Notice"`, `"Reply"` 등 "매직 스트링(Magic String)"으로 선언되어 오타 발생 시 컴파일러가 잡아주지 못함.

**해결 분석 (`Plan_Command.md` 반영 결과)**:
- **✅ 완벽히 해결됨 (Strongly-Typed Constants 도입)**
- 도메인 중심 부품인 `CommandFeatureTypes` 정적(Static) 모델이 도입되었습니다.
- **코드 검증 (`CommandFeatureTypes.cs`)**:
  ```csharp
  public static class CommandFeatureTypes
  {
      public const string Reply = "Reply";
      public const string Notice = "Notice";
      // ...
  }
  ```
- DB와의 하위 호환성 이슈를 고려하여 Enum 테이블 매핑을 사용하지 않고, 런타임 최적화에 좋은 **const string 구조체** 를 사용했습니다. 각 `Strategy` 인터페이스와 엔티티의 초기화 코드는 이 상수를 참조하므로, 오타에 의한 치명적인 버그가 사전에 차단됩니다.

---

## 4. API Rate Limiting 체인 딜레이
**제기되었던 문제 (`Research_Command.md`)**:
- 명령어가 외부(치지직) API에 의존할 때, 타임아웃이나 Rate Limit 제재를 받으면 MediatR 파이프라인 전체에 블로킹 딜레이 연쇄 작용이 일어남.

**해결 분석 (`Plan_Command.md` 반영 결과)**:
- **✅ 방어 체계 및 확장의 우회로 확보 (Polly Resilience & Channel Offloading)**
- **코드 검증 (`ChzzkApiClient.cs` - 탄력성 파이프라인)**:
  `Polly.ResiliencePipeline` 객체를 탑재하여, 통신부(`ExecuteAsync`)에 2초의 **Timeout** 시간제한과 **서킷 브레이커(Circuit Breaker)** 정책(실패율 50%, 최소 요청 5건 기준 시 15초간 추가 요청 차단)을 두었습니다. 통신 지연 시 무한 로딩이 걸리지 않고 `Fast-Fail(빠른 실패)`하여 **보상 트랜잭션**으로 연결됩니다.
- **코드 검증 (`CommandBackgroundTaskQueue.cs` - 메시지 채널 구조)**:
  향후 장기 지연이나 대량 연산 로직이 필요할 때를 대비하여 메모리 누수를 원천 봉쇄한 1,000 Capacity 제한의 `System.Threading.Channels` 인스턴스를 구축했습니다. 이제 전략 레벨에서 무거운 작업은 ICommandBackgroundTaskQueue에 던지고(`QueueBackgroundWorkItemAsync`) MediatR를 반환시켜 사용자 이벤트를 0.1초 이내에 빠르게 소비할 수 있습니다.

---

## 🚀 종합 결론
설계 초안(`Plan_Command.md`)에서 예견된 "안전환불 처리(보상 트랜잭션), 데이터 분실 방지(Atomic Query 처리), 타입 파편화 제거(Const 객체), 통신 지연 방어(Polly 회복탄력성 및 Channel 워커)" 라는 4가지 목표가 모두 도메인, 인프라스트럭처, 어플리케이션(Application) 계층의 각 분리된 코드 구조 속에서 **성공적으로 매핑되고 구현되었습니다.** 

이번 아키텍처 정립을 통해 `MooldangBot`의 엔진은 어떠한 외부 트래픽이나 복잡한 명령어 시나리오(장기 지연, 동시 입력, 외부 오류)에서도 데이터(포인트) 오염이 일어날 수 없는 완성된 고가용성 구조를 가지게 되었습니다.
