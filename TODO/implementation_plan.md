# MooldangBot 아키텍처 고도화: PointModule & UnifiedCommandHandler 리팩토링

본 계획은 Modular Monolith 아키텍처를 강화하고, 재화 관리의 안정성과 명령어 처리의 유연성을 확보하기 위해 `PointModule` 내부 리팩토링 및 `UnifiedCommandHandler`의 실행 파이프라인 고도화를 목표로 합니다.

## User Review Required

> [!IMPORTANT]
> **원자적 업데이트 (Atomic Update)**: Dapper를 사용하여 DB 레벨에서 직접 감산 연산을 수행합니다. `Points >= @Amount` 조건을 통해 마이너스 잔액 발생을 원천 차단합니다.
> **Short-circuiting Pipeline**: 결제 실패 시 이후 로직이 실행되지 않도록 핸들러 수준에서 즉시 종료합니다.
> **Cheese Compensation (Saga)**: 유료 재화(치즈) 결제 후 기능 실행 중 예외가 발생할 경우, 자동으로 재화를 복구해주는 보상 트랜잭션 로직을 포함합니다.

## Proposed Changes

---

### [MooldangBot.Contracts]

#### [NEW] [DeductCurrencyCommand.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Contracts/Point/Requests/Commands/DeductCurrencyCommand.cs)
- `DeductCurrencyCommand` 레코드 (StreamerUid, ViewerUid, Amount, CurrencyType)
- `DeductResult` 레코드 (Success, RemainingBalance, ErrorMessage)

#### [NEW] [IDbConnectionFactory.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Contracts/Common/Interfaces/IDbConnectionFactory.cs)
- Dapper 사용을 위한 `IDbConnection` 생성 인터페이스 정의.

---

### [MooldangBot.Infrastructure]

#### [MODIFY] [MariaDbService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/Persistence/MariaDbService.cs)
- `IDbConnectionFactory` 구현 추가 및 `CreateConnection` 공개(Public) 전환.

---

### [MooldangBot.Modules.Point]

#### [NEW] [DeductCurrencyCommandHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Modules.Point/Features/Commands/DeductCurrency/DeductCurrencyCommandHandler.cs)
- Dapper를 활용한 원자적 업데이트 SQL 구현.
- `viewer_points` 및 `viewer_donations` 테이블에 대한 조건부 차감 로직.

---

### [MooldangBot.Modules.Commands]

#### [MODIFY] [UnifiedCommandHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Modules.Commands/Handlers/UnifiedCommandHandler.cs)
- **Billing Pipeline** 이식: `CheckRole` -> `DeductCurrencyCommand` -> `StrategyExecution`.
- **HandleCompensationAsync**: 기능 실행 실패 시 `AddPointsCommand`를 호출하여 치즈 복구 로직 구현.

---

## Verification Plan

### Automated Tests
- `DeductCurrencyCommandHandler` 동시성 테스트: 잔액 100인 상태에서 10씩 20번 동시 요청 시 정확히 10번만 성공하는지 검증.
- `UnifiedCommandHandler` 파이프라인 테스트: 결제 실패 시 `Strategy.ExecuteAsync`가 호출되지 않는지 검증.

### Manual Verification
- 치즈 부족 시 적절한 에러 메시지가 채팅창에 출력되는지 확인.
- 결제 후 기능 실행 중 강제 예외 발생 시 치즈가 다시 복구되는지 확인.
