# 📑 MooldangBot EDMH Final Evolution Plan

이 문서는 MooldangBot의 아키텍처를 중앙 집중형 Orchestration에서 이벤트 기반 Choreography로 전환하고, 하부 인프라 안정성 및 분산 무결성을 확보하기 위한 최종 진화 계획을 담고 있습니다.

## 🎯 목표 (Goal)
현재의 중앙 집중식 구조를 해체하고, 각 모듈이 자율적으로 반응하는 **'이벤트 기반 자율 반응 시스템(Choreography)'**으로 전환하여 운영 안정성과 확장성을 극대화합니다.

---

## 🛠️ Phase 1: 기반 안정화 (Infrastructure Cleanup)
**목표: DB 초기화 스트레스 없는 영속적 보안 환경 구축**

1. **암호화 키 영속화 확정**
   - `DependencyInjection.cs`의 `PersistKeysToFileSystem` 설정을 확인하고, `/root/.aspnet/DataProtection-Keys` 경로를 강제하여 컨테이너가 재시작되거나 DB가 초기화되어도 기존 토큰을 읽을 수 있도록 보장합니다.
2. **스키마 클린업**
   - EF Core 마이그레이션을 통해 DB에서 더 이상 사용되지 않는 `DataProtectionKeys` 테이블을 완전히 삭제합니다.

## 📡 Phase 2: 내부 이벤트 안무 (Internal Event Choreography)
**목표: 핸들러 직접 호출 방식을 '도메인 이벤트' 전파 방식으로 전환**

1. **핵심 도메인 이벤트 정의**
   - `CommandMatchedEvent`, `CommandExecutedEvent`, `PointDeductedEvent` 등 모듈 간 소통의 언어를 규격화합니다.
2. **핸들러 결합도 제거 (Decoupling)**
   - `UnifiedCommandHandler`가 직접 모듈(Point, Roulette 등)을 호출하는 대신 이벤트를 발행(`Publish`)하고, 각 모듈이 `INotificationHandler`를 통해 스스로 반응하도록 리팩토링합니다.
3. **Primary 실행 로직 고도화**
   - 여러 명령어가 매칭될 경우, 우선순위가 가장 높은 'Primary'가 결제 및 메인 실행을 주도하도록 정교화합니다.

## 🕹️ Phase 3: 대시보드 동기화 (Studio UI)
**목표: 신규 백엔드 엔진(Salvo V2)의 제어 기능을 UI에 부여**

1. **DTO 및 저장 로직 업데이트**
   - `Priority`(우선순위), `MatchType`(매칭 방식), `RequiresSpace`(공백 요구) 필드를 Studio(SvelteKit) 프론트엔드와 백엔드 간에 완벽히 동기화합니다.
2. **명령어 관리 인터페이스 개선**
   - 스트리머가 명령어의 우선순위를 직관적으로 조절하고, 매칭 방식을 세밀하게 선택할 수 있는 UI를 구현합니다.

## 🐰 Phase 4: 메시지 브로커 통합 (RabbitMQ)
**목표: 대규모 트래픽 대응을 위한 비동기 처리 확장**

1. **MassTransit 외부 사출**
   - 내부 도메인 이벤트를 외부 브로커(RabbitMQ)로 사출하여 외부 워커(chzzk-bot 등)와의 유기적인 통신망을 구축합니다.
2. **부수 효과(Side-Effect) 분리**
   - 즉시 응답이 불필요한 로그 기록, 통계 집합, 알림 전송 등을 백그라운드 큐로 분리하여 메인 로직의 응답성을 향상시킵니다.

## 🛡️ Phase 5: 신뢰성 및 복구력 (Saga/Compensation)
**목표: 분산 환경에서의 데이터 무결성 보장**

1. **Saga State Machine 도입**
   - "포인트 차감 -> 기능 실행"으로 이어지는 복합 트랜잭션의 상태를 관리합니다.
2. **보상 트랜잭션(환불) 시스템**
   - 기능 실행 과정에서 오류 발생 시, 차감된 포인트를 자동으로 환불하는 `RefundCurrencyCommand`를 발행하여 데이터 일관성을 유지합니다.

---

## 🔍 검증 계획 (Verification)
- **단위 테스트**: MediatR 이벤트 발행/수신 및 Saga 상태 전이 시뮬레이션.
- **통합 테스트**: 실제 치지직 이벤트 수신 시 포인트 차감부터 보상 트랜잭션(환불)까지의 전 과정 확인.
- **UI 검증**: Studio 대시보드에서의 신규 필드 설정 및 실제 명령어 실행 반영 여부 확인.

---

> [!NOTE]
> 작성자: Senior Full-Stack Partner '물멍' (MooldangBot Team)
> 작성일: 2026-04-14
