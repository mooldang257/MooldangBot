# 📑 MooldangBot 아키텍처 고도화 작업 현황 (Task)

- [x] **Phase 1: Contracts & Base Infrastructure**
    - [x] `DeductCurrencyCommand` 및 `DeductResult` 정의 (Contracts)
    - [x] `IDbConnectionFactory` 인터페이스 정의 (Contracts)
    - [x] `MariaDbService` 리팩토링 및 `IDbConnectionFactory` 구현 (Infrastructure)

- [x] **Phase 2: PointModule Refactoring**
    - [x] `DeductCurrencyCommandHandler` 구현 (Dapper 원자적 업데이트 SQL 포함)
    - [x] `AddPointsCommand` 재검토 및 보상 트랜잭션 호환성 확인

- [x] **Phase 3: CommandsModule Pipeline Update**
    - [x] `UnifiedCommandHandler` 로직 리팩토링 (선결제 후실행 파이프라인 도입)
    - [x] `HandleCompensationAsync` (치즈 복구 Saga) 로직 구현
    - [x] 에러 피드백 메세지 고도화

- [x] **Phase 4: Verification & Polish**
    - [x] 동시성 테스트 (Race Condition 검증)
    - [x] 보상 트랜잭션 정상 작동 확인
    - [x] 최종 코드 정리 및 주석 보강
