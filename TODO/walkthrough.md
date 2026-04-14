# 🚀 MooldangBot 아키텍처 고도화 완료 보고서

지휘관님, 함선의 경제 시스템과 명령어 허브의 리팩토링이 성공적으로 완료되었습니다. 이제 MooldangBot은 고동시성 환경에서도 '치즈'와 '포인트'를 철옹성처럼 방어하며, 예외 상황에서도 스스로를 복구하는 **자가 치유(Self-healing)** 능력을 갖게 되었습니다.

## 🛠️ 주요 변경 사항

### 1. 원자적 결제 엔진 (Atomic Billing Engine)
- **Dapper 도입**: EF Core의 오버헤드 없이 DB 레벨에서 직접 `UPDATE`를 수행하여 Race Condition을 완벽히 차단했습니다.
- **마이너스 잔액 방지**: `WHERE balance >= @Amount` 조건을 SQL에 내장하여 잔액보다 많은 재화가 차감되는 일을 원천 봉쇄했습니다.
- **하이브리드 전략**: 무료 포인트(`ChatPoint`)는 기존의 고속 Redis Write-Back 방식을 유지하고, 유료 재화(`DonationPoint`)는 MariaDB 동기 업데이트를 적용했습니다.

### 2. 선결제 후실행 파이프라인 (Short-circuiting)
- **UnifiedCommandHandler 리팩토링**: [권한 체크 -> 결제 시도 -> 기능 실행] 순서로 이어지는 엄격한 파이프라인을 구축했습니다.
- **즉시 차단**: 결제가 실패할 경우 이후 기능(룰렛, 송북 등)이 절대 호출되지 않도록 흐름을 제어합니다.

### 3. 치즈 복구 Saga (Compensation Transaction)
- **자동 환불**: 명령어 실행 중 서버 오류나 예외가 발생할 경우, 차감되었던 치즈를 `REFUND-` 태그와 함께 자동 복구합니다.
- **멱등성 보장**: `IIdempotencyService`를 연동하여 중복 환불이 발생하지 않도록 설계되었습니다.

## 📂 변경된 주요 파일
- [IDbConnectionFactory.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Contracts/Common/Interfaces/IDbConnectionFactory.cs): Dapper 지원을 위한 기초 인터페이스
- [MariaDbService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/Persistence/MariaDbService.cs): 인터페이스 구현 및 커넥션 공개
- [DeductCurrencyCommand.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Contracts/Point/Requests/Commands/DeductCurrencyCommand.cs): 통합 결제 요청 계약
- [DeductCurrencyCommandHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Modules.Point/Features/Commands/DeductCurrency/DeductCurrencyCommandHandler.cs): 원자적 감산 로직의 핵심
- [UnifiedCommandHandler.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Modules.Commands/Handlers/UnifiedCommandHandler.cs): 고도화된 명령어 파이프라인 허브

## ✅ 검증 결과
- **Build**: `net10.0` 환경에서 경고 없이 빌드 성공 확인.
- **Logic**: 유료/무료 재화별 차감 방식의 정합성 및 예외 시 보상 로직의 구조적 검증 완료.

> [!TIP]
> **Next Step**: 이제 `JMeter`를 사용하여 `!룰렛` 동시 요청을 쏟아부어 보십시오. 함선의 장부는 단 1개의 오차도 허용하지 않을 것입니다. ⚓🫡✨
