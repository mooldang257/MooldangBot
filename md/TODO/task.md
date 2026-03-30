# Task: Phase 1 즉시 안정화 구현 [x] (v3.0.0 완료)

- [x] 1-0. NuGet 패키지 추가 (Microsoft.Extensions.Http.Resilience, HealthChecks)
- [x] 1-1. 순차 루프 → 병렬 배치 처리 전환
  - [x] ChzzkBackgroundService.cs: foreach → Parallel.ForEachAsync
  - [x] SystemWatchdogService.cs: foreach → Parallel.ForEachAsync
- [x] 1-2. Task.Run → Channel<T> 기반 역압 처리
  - [x] ChatEventItem 모델 생성
  - [x] ChatEventChannel 서비스 생성 (Bounded Channel)
  - [x] ChatEventConsumerService 백그라운드 서비스 생성
  - [x] ChzzkChatClient.cs: Task.Run → Channel 큐잉으로 교체
- [x] 1-3. Graceful Shutdown 설정 (Program.cs)
- [x] 1-4. DB 커넥션 풀 → DbContext Pooling 전환
- [x] 1-5. 헬스체크 엔드포인트 추가
- [x] 빌드 검증 (dotnet build)
- [x] Implementation_Plan_V1.md Phase 1 완료 표시
- [x] Research.md 업데이트 (확장성 분석 결과 및 Phase 1 구현 내역)

# Task: DB 마이그레이션 [x]
 - [x] 2-4. **최종 검증**: 서버 실행(`dotnet run`) 및 병렬 세션(2인 이상) 동시 접속/구독 확인
 
 # Task: Phase 2 구조 고도화 [백업]
 
 - [x] 2-1. WebSocket 매니저 세그먼트화 (Sharding)
   - [x] IWebSocketShard 인터페이스 및 Shard 구현
   - [x] ShardedWebSocketManager 도입 및 기존 엔진 교체
 - [x] 2-2. 레거시 코드 완전 제거
   - [x] ChzzkChannelWorker.cs 삭제 및 참조 정리
 - [x] 2-3. SignalR 그룹 라우팅 강화
   - [x] OverlayHub 및 ChatEventConsumerService 내 그룹 전송 강화
 - [x] 2-4. 토큰 갱신 전용 독립 서비스 분리
   - [x] TokenRenewalBackgroundService 신규 생성
   - [x] 우선순위 기반(만료 임박순) 갱신 로직 구현
 - [x] 2-5. 최종 검증 및 문서화
   - [x] Research.md 업데이트
   - [x] Phase 2 완료 보고서 작성
 - [x] 2-6. [핫픽스] 화면 접근 권한(ChannelManager) 정책 복구
 - [x] 2-0. 미진행 마이그레이션 확인 및 적용
 - [x] 2-1. 마이그레이션 오류 해결 (IX_streameromakases 등)
 - [x] 2-2. **최종 복구**: DesignTimeDbContextFactory 타겟 DB 교정 (ChzzkSongBook_Dev -> MooldangBot)
 - [x] 2-3. **최종 복구**: 중복 테이블(broadcastsessions 등) 충돌 해결 및 전체 테이블 생성 완료
 - [x] 2-4. **최종 검증**: 서버 실행(`dotnet run`) 및 병렬 세션(2인 이상) 동시 접속/구독 확인

### Phase 2 (Step-by-Step) 🏗️
 - [x] Step 1. 필요 NuGet 패키지 설치
   - [x] Websocket.Client (5.*)
   - [x] EFCore.BulkExtensions (8.*)
   - [x] Serilog.AspNetCore (9.*)
 - [x] Step 2. WebSocket 매니저 → Websocket.Client로 교체
   - [x] WebSocketShard 리팩토링 및 핑/리시브 루프 대체
 - [x] Step 3. 레거시 코드 완전 제거 및 엔진 단일화
  - [x] Step 4. WebSocket 매니저 세그먼트화 (최적화)
  - [x] Step 3-B. 데드코드(ChzzkChatClient) 삭제 및 엔진 단일화 완료 (v3.6.5)

## Phase 3: 멀티 서버 분산 스케일링 (Horizontal Scaling)
- [x] Step 0. Redis 인프라 NuGet 패키지 설치
- [x] Step 1. 결정론적 해싱 (XxHash32) 도입 및 가상 샤딩 리팩토링
- [x] Step 2. SignalR Redis Backplane 통합
- [x] Step 3. 분산 락 (RedLock) 기반 중복 연결 방지 구현
- [x] Step 4. 분산 캐시 (IDistributedCache) 전환
- [x] Step 5. Docker Compose Scale Up 및 환경 변수 최적화 완료
- [x] Step 3-1. 다중 인스턴스 배포 (Replicas 4) 및 환경 변수 연동 완료
- [x] Step 3-3. 메시지 큐 (RabbitMQ) 도입 및 인프라 구성 완료
- [x] 개선사항 #5. Serilog Sink (파일/콘솔) 고도화 완료
- [x] 개선사항 #6. WebSocketShard 단위 Health Metric 실장 완료
- [x] 개선사항 #6. WebSocketShard 단위 Health Metric 실장 완료

## Phase 3.5: 분산 안정성 강화 및 인프라 완성 (Next Steps)

### 🔴 [Critical] SHARD_INDEX 전적 자동 할당 (Self-Registration)
- [x] Redis 기반 인스턴스 인덱스 자동 점유 로직 구현
- [x] 점유된 인덱스에 대한 주기적 [Heartbeat](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Services/Philosophy/BroadcastScribe.cs#67-100) (TTL 갱신) 처리 완료

### 🟠 [High] Docker 인프라 정의 및 의존성 보완
- [x] [docker-compose.yml](file:///c:/webapi/MooldangAPI/MooldangBot/docker-compose.yml): Redis 전용 서비스 정의 추가
- [x] [docker-compose.yml](file:///c:/webapi/MooldangAPI/MooldangBot/docker-compose.yml): `app` 서비스에 `rabbitmq` 및 `redis` 의존성(`depends_on`) 보완
- [x] [.env](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Api/.env): Redis 접속 정보 통합 관리

### 🟡 [Medium] RabbitMQ 기반 비동기 워크플로우 실장
- [x] `IRabbitMqService` 인터페이스 및 추상화 구현체 작성
- [x] 채팅 이벤트 발생 시 RabbitMQ Exchange로 메시지 발행(Publish) 로직 연동
- [x] [BackgroundService](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/TokenRenewalBackgroundService.cs#16-90)를 통한 메시지 소비(Consume) 및 확장성 검증

### 🟡 [Medium] 인프라 안정성 및 락(Lock) 고도화
- [x] [DependencyInjection.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/DependencyInjection.cs): Redis 연결(`ConnectionMultiplexer`) 비동기/Lazy 전환
- [x] `RedLock`: 장시간 연결 시 락 연장(Extend) 로직 또는 적정 만료 시간(Expiry) 재설계

### 🟢 [Low] 코드 가독성 및 빌드 경고 제거
- [x] [Program.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Api/Program.cs): 중복 `using System.Text.Json` 제거 및 `RedisChannel.Literal` 명시적 사용
- [x] [Api.csproj](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Api/MooldangBot.Api.csproj): 불필요한 패키지 참조(`Microsoft.Extensions.Diagnostics.HealthChecks` 등) 정리
- [x] 기타 `null` 허용 여부 및 파라미터 미사용 경고 해결

---

## 💻 주요 설계 스니펫 (Pre-Analysis)

#### 1. SHARD_INDEX 자동 할당 (Redis 기반)
```csharp
// [예시 로직]
for (int i = 0; i < maxCount; i++) {
    if (await db.LockTakeAsync($"shard:index:{i}", instanceId, expiry)) {
        _shardIndex = i;
        break;
    }
}
```

#### 2. RabbitMQ 발행 구조
```csharp
// [예시 로직]
var body = MessagePackSerializer.Serialize(chatEvent);
await channel.BasicPublishAsync(exchange: "chat.raw", routingKey: "", body: body);
```
