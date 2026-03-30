# Task_V.2: 시스템 최적화 및 대규모 운영 대비 (Phase 4)

[Validation.md](file:///c:/webapi/MooldangAPI/MooldangBot/md/TODO/Validation.md)의 용량 분석 결과 도출된 병목 지점 및 잠재적 장애 요인을 해결하기 위한 최적화 작업 목록입니다.

## 🔴 [Critical] 즉시 수정 필요 (안정성)

- [ ] **ShardedWebSocketManager 하트비트 루프 보완**
    - [ ] [StartHeartbeat()](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/Sharding/ShardedWebSocketManager.cs#102-138) 내 `while` 루프에 `await Task.Delay(TimeSpan.FromSeconds(10))` 추가
    - [ ] 루프 내 `CancellationToken` 정합성 재검증

## 🟡 [Medium] 리소스 풀 및 병렬성 최적화 (성능)

- [ ] **비동기 이벤트 소비자 처리량 확대**
    - [ ] [ChatEventConsumerService](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/ChatEventConsumerService.cs#16-171)의 `ConsumerCount`를 `3`에서 `8`로 상향
    - [ ] 채널 병목 현상 모니터링 로그 추가
- [ ] **데이터베이스 커넥션 풀 확장**
    - [ ] [DependencyInjection.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/DependencyInjection.cs)의 `AddDbContextPool` `poolSize`를 `128`에서 `256`으로 상향
- [ ] **Infrastructure 지연 초기화 고도화**
    - [ ] `IConnectionMultiplexer` 등록을 `Lazy<IConnectionMultiplexer>` 또는 [Async](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/LogBulkBufferWorker.cs#43-49) 팩토리 패턴으로 전환
    - [ ] Redis 연결 실패 시 앱 부팅 속도 영향 최소화 검증
- [ ] **비동기 자원 해제(Async Disposal) 실장**
    - [ ] [IWebSocketShard](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/Sharding/IWebSocketShard.cs#6-15) 및 [WebSocketShard](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/Sharding/WebSocketShard.cs#33-40)를 `IAsyncDisposable`로 전환
    - [ ] [Dispose()](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/Sharding/WebSocketShard.cs#168-175) 내 동기 `Wait()` 호출을 `await DisposeAsync()`로 교체하여 Graceful Shutdown 안정성 확보

## 🟢 [Low] 코드 품질 및 가시성 강화 (유지보수)

- [ ] **중복 로직 및 데드코드 정리**
    - [ ] [Program.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Api/Program.cs) 내 중복된 `db.SaveChanges()` 호출 제거
- [ ] **구조화 로깅(Structured Logging) 표준화**
    - [ ] 주요 서비스([ShardedWebSocketManager](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/Sharding/ShardedWebSocketManager.cs#33-63), [WebSocketShard](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/Sharding/WebSocketShard.cs#33-40))의 로그 메시지를 구조화된 템플릿 형태로 전환
- [ ] **시스템 헬스체크 통합 가시성 확보**
    - [ ] [BotHealthCheck](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/Services/BotHealthCheck.cs#17-21)에 Redis 및 RabbitMQ 연결 상태 체크 로직 통합
    - [ ] `/healthz` 응답에 인프라 메트릭(Latency 등) 추가 검토
- [ ] **RabbitMQ 채널 관리 최적화**
    - [ ] [RabbitMqService](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/Services/RabbitMqService.cs#14-105) 내 단일 채널 병목 가능성 대비 연동 구조 재검토

---

## 📈 검증 시나리오 (Verification)

1. **CPU 점유율 확인**: 하트비트 `Task.Delay` 적용 후 유휴 상태 CPU 사용률이 정상 범위(1~3%)로 내려오는지 확인
2. **부하 테스트**: 200개 가상 WebSocket 연결 시뮬레이션 시 DB 풀 고갈 및 메시지 누락 여부 점검
3. **종료 테스트**: `SIGTERM` 신호 전달 시 200개 연결이 30초 내에 비동기로 안전하게 닫히는지 확인
