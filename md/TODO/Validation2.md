# MooldangAPI 2차 검증 및 아키텍처 감사 보고서 (Validation2.md)

> **검증일**: 2026-03-30  
> **검증자**: 물멍 (Senior Full-Stack AI Partner)  
> **대상**: `Validation.md` 내 지적 사항(N1~N8) 및 3대 고도화 기술 적용 현황

---

## 1. 지적 사항 (N1 ~ N8) 최종 이행 상태

| ID | 지적 사항 (Summary) | 상태 | 코드 근거 (Location) | 비고 |
|:---:|:---|:---:|:---|:---|
| **N1** | BroadcastScribe 셧다운 플러시 누락 | **완료** | `BroadcastScribe.cs:31`, `OnShutdown` | `.Register(OnShutdown)`으로 구현 확인 |
| **N2** | OverlayHub 자동 그룹 가입 부재 | **완료** | `OverlayHub.cs:21-41` | `OnConnectedAsync`에서 QueryString 기반 자동 가입 |
| **N3** | WebSocketManager 동기 Dispose 블로킹 | **완료** | `ShardedWebSocketManager.cs` | `IAsyncDisposable`만 구현하여 블로킹 제거 |
| **N4** | WebSocketShard 동기 Dispose 블로킹 | **완료** | `WebSocketShard.cs` | `ValueTask DisposeAsync` 기반 비동기 자원 해제 |
| **N5** | ChzzkBackgroundService Captive Dependency | **완료** | `ChzzkBackgroundService.cs:41, 60` | `IServiceScopeFactory`를 통한 독립 Scope 생성 |
| **N6** | PeriodicMessageWorker 타임존 의존성 | **완료** | `PeriodicMessageWorker.cs:51` | `DateTimeOffset.UtcNow` 사용으로 타임존 독립화 |
| **N7** | Infrastructure 패키지 오염 (HealthChecks) | **완료** | `MooldangBot.Api/Health/BotHealthCheck.cs` | Api 레이어로 이전 및 의존성 제거 완료 |
| **N8** | SignalR MessagePack 미도입 | **보류** | `Program.cs:157` | 현재 JSON 프로토콜 사용 중 (의도적 유지 가능성) |
| **#6** | PeriodicMessageWorker N+1 쿼리 | **완료** | `PeriodicMessageWorker.cs:46-50` | `Contains` 및 `ToLookup`을 통한 배치 조회 구현 |
| **#7** | BackgroundService 중복 실행 방지 | **완료** | `ChzzkBackgroundService.cs:14, 30` | `SemaphoreSlim(1, 1)` 기반 재진입 방지 로직 적용 |

---

## 2. 3대 핵심 고도화 기술 적용 현황

### 2-1. Dapper Hybrid (고빈도 트랜잭션 최적화)
- **적용 상태**: **완료 (핵심 도메인 적용)**
- **증명**: `PointTransactionService.cs:33-57`에서 MariaDB `ON DUPLICATE KEY UPDATE` 원자적 쿼리를 Dapper로 수행. EF Core의 무거운 오버헤드 없이 고빈도 포인트 적립 가능.

### 2-2. Polly Resilience (복원력 파이프라인)
- **적용 상태**: **부분 적용 (과도기)**
- **증명**: 
    - `ChzzkApiClient.cs:34-43`에서 `ResiliencePipeline` (Timeout, CircuitBreaker) 정의.
    - `SendChatInternalAsync`, `UpdateLiveSettingAsync` 등 쓰기 작업에 적용 완료.
    - `DependencyInjection.cs:65`에서 `AddStandardResilienceHandler()`를 통한 전역 정책 병행 사용 중.

### 2-3. JSON Source Generator (High Performance)
- **적용 상태**: **완료**
- **증명**: 
    - `ChzzkJsonContext.cs` 소스 생성기 컨텍스트 구축 완료.
    - `ChzzkApiClient.cs`에서 리플렉션 없이 `JsonTypeInfo`를 통한 직접 역직렬화 수행.
    - `Program.cs`에서 `TypeInfoResolverChain`에 등록하여 전역 API 성능 최적화 완료.

---

## 3. 분산 아키텍처 및 동시성 검증

### 3-1. 수평 확장성 (Distribution Guard)
- `ShardedWebSocketManager`에서 `xxHash32` 기반 결정론적 샤딩 확인.
- `REDIS_URL` 환경변수 기반 SignalR Backplane 및 분산 캐시 설정 완료 (`Program.cs:155-171`).
- `RedLock` 패키지 도입 확인 (`Infrastructure.csproj:29`), 멀티 노드 간 중복 소켓 방지 준비 완료.

### 3-2. 역압 처리 (Backpressure)
- `ChatEventChannel`을 통한 `BoundedChannel` (Capacity: 2000) 도입 확인.
- `ChatEventConsumerService`에서 8개의 병렬 소비자 가동으로 처리량 극대화 확인 (`ChatEventConsumerService.cs:22`).

---

## 4. 최종 권장 사항 (Senior Memo)

1.  **[N7] 패키지 정리**: `Infrastructure` 프로젝트에서 `HealthChecks` 패키지를 제거하고 `Api` 프로젝트에서만 관리하도록 수정이 필요합니다. (계층 오염 방지)
2.  **[고도화] JSON SG 도입**: `ChzzkJsonContext` (Source Generator)를 생성하여 DTO 직렬화 성능을 극대화할 것을 강력히 권장합니다.
3.  **[회복 탄력성] 파이프라인 확장**: `ChzzkApiClient`의 읽기 작업(`GetChannelInfo` 등)에도 정의된 Polly 파이프라인을 전면 적용하여 서킷 브레이커의 효용을 높여야 합니다.
4.  **[동시성] Redis Lock 실장**: `RedLock` 패키지는 등록되어 있으나 실제 `ShardedWebSocketManager` 내부에서 `InitializeAsync` 외에 개별 채널 연결 시의 분산 락 로직은 보강이 필요해 보입니다.
