# Step 4 진행 결과 보고서: 백그라운드 동시성 제어 및 컨슈머 확장

## 1. 개요
백그라운드 서비스의 중복 실행을 방지하여 시스템 안정성을 높이고, 메시지 컨슈머 확장을 통해 실시간 채팅 처리 능력을 강화하였습니다.

## 2. 주요 변경 사항

### 2.1 백그라운드 재진입 방지 (SemaphoreSlim 도입)
- **적용 대상**: `SystemWatchdogService.cs`, `ChzzkBackgroundService.cs`
- **구현 세부 사항**:
    - 각 서비스에 `private readonly SemaphoreSlim _semaphore = new(1, 1);` 필드 추가.
    - `ExecuteAsync` 루프 시작 시 `_semaphore.WaitAsync(0)`를 호출하여 이미 작업이 진행 중인지 확인.
    - **Skip 로직**: 이전 주기가 완료되지 않았을 경우 이번 주기를 건너뛰며 경고 로그를 남김.
    - **가드 로직**: `try-finally` 블록을 사용하여 작업 성공/실패 여부와 관계없이 세마포어를 해제(`Release`)하도록 보장.
- **효과**: 시스템 부하 급증 시 작업이 누적되어 연쇄적으로 장애가 발생하는 현상을 원천 차단.

### 2.2 ChatEventConsumerService 확장
- **변경 사항**: `ConsumerCount` 상수를 `3`에서 **`8`**로 상향 조정.
- **효과**:
    - RabbitMq 및 내부 채널의 이벤트 소비 속도 향상.
    - 대규모 시청자 유입 시 발생할 수 있는 채팅 응답 지연(Latency) 최소화.
    - 병렬 처리량을 2.6배 상향하여 시스템 처리 능력 극대화.

## 3. 검증 결과
- **빌드 검증**: `MooldangBot.Application` 프로젝트 빌드 성공.
- **로직 검증**:
    - `SemaphoreSlim`의 `WaitAsync(0)` 반환값에 따른 분기 처리 정상 작동 확인.
    - 다중 소비자(8개) 가동 로그 확인 완료.

## 4. 향후 고려 사항
- 소비자 수 상향에 따른 MariaDB 연결 수(Connection Pool) 모니터링 필요.
- CPU 사용량이 임계치를 넘을 경우 소비자 수를 5~6개로 미세 조정 검토.
