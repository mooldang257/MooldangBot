# MooldangBot 시스템 아키텍처 심층 분석 및 개선점 연구 보고서

> **분석 일시**: 2026-03-30 KST  
> **대상 서버**: i5-12400 (6C/12T), 32GB DDR4, NVMe 3.0 2TB SSD, 500Mbps  
> **목표 수용량**: 스트리머 100명 × 평균 시청자 300명 (동시 채팅 메시지 약 3,000~5,000 msg/s)

---

## 목차
1. [서버 용량 산정 및 병목 분석](#1-서버-용량-산정-및-병목-분석)
2. [핵심 코드 결함 분석 (물멍 발견)](#2-핵심-코드-결함-분석-물멍-발견)
3. [사용자 제시 3대 이슈 심층 분석](#3-사용자-제시-3대-이슈-심층-분석)
4. [종합 위험도 매트릭스](#4-종합-위험도-매트릭스)
5. [권장 개선 로드맵](#5-권장-개선-로드맵)

---

## 1. 서버 용량 산정 및 병목 분석

### 1.1 리소스 소비 추정

| 리소스 | 현재 (1채널) | 100채널 예상 | 서버 한계 | 위험도 |
|:---|:---|:---|:---|:---|
| **WebSocket 연결** | 1개 ClientWebSocket + Ping/Receive Task 2개 | 100개 소켓 + 200개 Task | Thread Pool 기본 12스레드 → **Task 기아(Starvation)** | 🔴 Critical |
| **메모리** | ~50MB (앱 + DB Pool) | ~2.5GB (소켓 버퍼 16KB×100 + ConcurrentDict + EF Core 캐시) | 32GB | 🟡 Warning |
| **DB 동시 연결** | MaxPoolSize=100 (기본) | Scoped 서비스 100채널 × 초당 3~5 쿼리 = **300~500 QPS** | MariaDB max_connections=151 (기본) → **풀 고갈** | 🔴 Critical |
| **CPU (i5-12400)** | ~5% idle | JSON 파싱(3,000 msg/s) + MediatR Publish + DB I/O = **60~80%** | 6C/12T | 🟡 Warning |
| **네트워크** | ~1 Mbps | 100 WebSocket + SignalR + API 호출 ≈ **50~100 Mbps** | 500Mbps | 🟢 Safe |

### 1.2 핵심 병목 지점

#### 🔴 A. Thread Pool 기아 (Task Starvation)
- **위치**: `ChzzkChatClient.ConnectAsync()` (L110)
- **원인**: `_ = Task.Run(async () => { ... })` 로 채널당 2개의 장기 실행 Task(Receive/Ping)를 생성합니다. 100채널이면 **200개 Task**가 ThreadPool을 점유합니다.
- **영향**: i5-12400의 ThreadPool 기본 최소 스레드 수는 12개입니다. 200개 Task가 동시에 `await`에서 복귀하면 ThreadPool 큐에 적체되어 **모든 비동기 작업(API 응답, DB 쿼리 등)이 지연**됩니다.

#### 🔴 B. DB Connection Pool 고갈
- **위치**: `DispatchEventAsync()` (L234), `HandleEventAsync()` (L238)
- **원인**: 채팅 메시지 1건마다 `_scopeFactory.CreateScope()` → `IAppDbContext` 생성 → `StreamerProfiles` 조회를 수행합니다.
- **영향**: 초당 3,000건의 채팅이 발생하면 MariaDB의 기본 `max_connections=151`을 즉시 초과하여 `MySqlException: Too many connections` 발생.

#### 🟡 C. N+1 쿼리 패턴
- **위치**: `PeriodicMessageWorker.ExecuteAsync()` (L39-44)
- **원인**: `profiles` 목록을 순회하며 각 프로필마다 `PeriodicMessages`를 개별 조회합니다.
- **영향**: 100명 스트리머 → 101번의 DB 쿼리가 1분마다 실행됩니다.

#### 🟡 D. Singleton에서 Scoped 서비스 캡처 위험
- **위치**: `SystemWatchdogService.MonitorAndRenewPulseAsync()` (L46-47)
- **원인**: Singleton인 `IBroadcastScribe`와 `IChzzkChatClient`를 Scoped `scope.ServiceProvider`로 resolve하고 있습니다. 이 서비스들은 이미 `DependencyInjection.cs`에서 `AddSingleton`으로 등록되어 있어 현재는 문제가 없지만, **향후 Scoped로 변경 시 즉시 `InvalidOperationException`이 발생**합니다.

---

## 2. 핵심 코드 결함 분석 (물멍 발견)

### 2.1 🔴 이중 소켓 아키텍처 (ChzzkChannelWorker + ChzzkChatClient 공존)

현재 시스템에는 동일한 역할(WebSocket 연결 및 채팅 수신)을 수행하는 **두 개의 독립적인 클래스**가 공존합니다.

| | ChzzkChannelWorker | ChzzkChatClient |
|:---|:---|:---|
| **위치** | Application/Workers/ | Infrastructure/ApiClients/Philosophy/ |
| **DI 수명** | 수동 생성 (new) | Singleton |
| **사용처** | `ChzzkBackgroundService`에서 `new ChzzkChannelWorker()` | `ChzzkBotService.EnsureConnectionAsync()` |
| **소켓 관리** | 로컬 `ClientWebSocket` 변수 | `ConcurrentDictionary<string, ClientWebSocket>` |

**문제점**: 
- `ChzzkBackgroundService`가 `ChzzkChannelWorker`를 `new`로 생성하면서 **동시에** `ChzzkBotService.EnsureConnectionAsync()`가 `ChzzkChatClient`를 통해 같은 채널에 연결을 시도하면 **하나의 채널에 두 개의 WebSocket이 열립니다**.
- 결과: **MediatR 이벤트 중복 발행** (채팅 메시지가 2번 처리됨).

### 2.2 🔴 `DisconnectAsync` 호출 시 Race Condition

```
// ChzzkChatClient.cs L339-358
public async Task DisconnectAsync(string chzzkUid)
{
    if (_ctsList.TryRemove(chzzkUid, out var cts))
    {
        cts.Cancel();   // ← 여기서 ReceiveLoopAsync가 예외를 잡고...
        cts.Dispose();
    }
    // ...
    if (_clients.TryRemove(chzzkUid, out var ws))
    {
        // ... ws.CloseAsync() + ws.Dispose()
    }
}
```

**문제점**:
- `ReceiveLoopAsync`의 `finally` 블록(L188)에서도 `DisconnectAsync(chzzkUid)`를 호출합니다.
- `ConnectAsync` 내부에서도 맨 처음에 `await DisconnectAsync(chzzkUid)`를 호출합니다 (L75).
- 이 세 곳이 동시에 실행되면 `ConcurrentDictionary.TryRemove()`가 이미 제거된 키를 다시 제거하려 하면서 **WebSocket 객체가 Dispose되지 않은 채 GC에 의존**하는 좀비 상태가 됩니다.

### 2.3 🟡 `CancellationToken.None` 사용 (Fire-and-Forget 위험)

```
// ChzzkChatClient.cs L220
await DispatchEventAsync(chzzkUid, json, CancellationToken.None);
```

**문제점**: 서버 종료(Graceful Shutdown) 시에도 이 Task는 취소되지 않습니다. DB에 대한 `SaveChangesAsync()`가 진행 중일 때 프로세스가 강제 종료되면 **데이터 손실**이 발생합니다.

### 2.4 🟡 `MemoryStream` 수신 버퍼의 비효율적 할당

```
// ChzzkChatClient.cs L162
using var ms = new MemoryStream();
```

**문제점**: 매 패킷마다 새로운 `MemoryStream`을 생성합니다. 초당 3,000건이면 **초당 3,000번의 힙 할당과 GC 압박**이 발생합니다. `ArrayPool<byte>` 또는 `RecyclableMemoryStream`을 사용해야 합니다.

### 2.5 🟡 BackgroundService 폴링 간격의 비효율

| 워커 | 현재 폴링 주기 | 100채널 시 문제 |
|:---|:---|:---|
| `ChzzkBackgroundService` | 1분 | 100명 순회 × API 호출(IsLive) = **매분 100+ HTTP 요청** + 지터(1~5초) = 전체 순환에 최대 **8분 소요** |
| `PeriodicMessageWorker` | 1분 | 100명 × N개 메시지 = **DB 쿼리 100~300회/분** |
| `SystemWatchdogService` | 1분 (내부 지터 1~5초) | 100명 × 토큰 체크 + 재연결 = **최대 8분** (폴링 주기보다 실행 시간이 길어짐) |
| `RouletteResultWorker` | 2초/30초 | 낮은 영향 |

**핵심 문제**: `SystemWatchdogService`와 `ChzzkBackgroundService`의 실행 시간(최대 8분)이 폴링 주기(1분)를 초과하면 **중첩 실행**이 발생합니다. `SemaphoreSlim` 등으로 재진입을 방지해야 합니다.

---

## 3. 사용자 제시 3대 이슈 심층 분석

### 3.1 🔴 웹소켓 매니저 (좀비 커넥션 및 중복 수신 방지)

#### 현재 상태
- `ChzzkChatClient`는 `ConcurrentDictionary`로 채널별 소켓을 관리하고, 1분 무활동 시 좀비 감지를 수행합니다 (L48-69).
- 그러나 `DisconnectAsync()`가 3곳에서 동시호출 가능하여 **Race Condition**이 존재합니다.
- `ChzzkChannelWorker`가 독립적으로 소켓을 열기 때문에 **중복 연결 방지가 불가능**합니다.

#### 해결 방향
1. **`SemaphoreSlim` 기반 채널별 잠금**: `DisconnectAsync()`와 `ConnectAsync()`에 채널별 `SemaphoreSlim`을 도입하여 동시 접근을 직렬화합니다.
2. **상태 머신(State Machine) 도입**: `Disconnected → Connecting → Connected → Disconnecting` 4단계 상태 전이를 엄격하게 관리합니다.
3. **ChzzkChannelWorker 제거**: 이중 소켓 아키텍처를 청산하고, `ChzzkChatClient` 단독 구조로 일원화합니다.

#### 위험 요소
- `ChzzkChannelWorker` 삭제 시 `ChzzkBackgroundService`의 `foreach` 루프 내부 로직을 `ChzzkChatClient`로 완전히 이전해야 합니다.
- 이전 중 이벤트 구독(`SubscribeEventAsync`) 로직의 토큰 소스(봇 vs 스트리머)가 달라지므로 **인증 체계 통합 검증**이 필수입니다.

### 3.2 🟡 SignalR 최적화 (그룹 라우팅 및 클라이언트 누락 방지)

#### 현재 상태
- `OverlayHub`에 `JoinStreamerGroup(chzzkUid)` / `LeaveStreamerGroup()` 메서드가 이미 구현되어 있습니다 (L29-38).
- 그러나 **프론트엔드에서 연결 직후 자동 구독을 수행하는 로직이 없으면**, 오버레이가 그룹에 조인하지 않은 상태에서 메시지를 수신하지 못합니다.

#### 해결 방향
1. **프론트엔드 자동 구독**: SignalR 연결 성공 콜백(`onConnected`)에서 즉시 `JoinStreamerGroup`을 호출합니다.
2. **재연결 시 자동 복구**: SignalR의 `withAutomaticReconnect()` 옵션과 `onreconnected` 콜백에서 그룹 재구독을 수행합니다.
3. **서버 측 강제 구독**: `OnConnectedAsync()` 오버라이드에서 쿼리스트링의 `chzzkUid`를 읽어 자동으로 그룹에 조인합니다.

#### 위험 요소
- 100명 스트리머 × 평균 2개 오버레이 = **200개 SignalR 커넥션**은 단일 서버에서 충분히 처리 가능합니다. (SignalR 기본 한계: ~5,000 연결)
- 다만 `Clients.Group(uid).SendAsync()`는 그룹 내 모든 연결에 순차적으로 전송하므로, 대기열이 길어지면 지연이 쌓일 수 있습니다. **MessagePack 프로토콜**로 전환하면 페이로드 크기를 50% 이상 줄일 수 있습니다.

### 3.3 🔴 안전한 종료 (Graceful Shutdown 및 타임아웃 방지)

#### 현재 상태
- `Program.cs`에 `HostOptions.ShutdownTimeout` 설정이 **없습니다**. 기본값은 **5초**입니다.
- 어떤 `BackgroundService`에서도 `StopAsync()` 오버라이드가 없으며, `ExecuteAsync`의 `stoppingToken`이 취소되면 즉시 루프를 빠져나갑니다.
- `BroadcastScribe`의 `_activeStats` (메모리 내 채팅 집계 데이터)는 **종료 시 DB에 플러시되지 않습니다**.

#### 해결 방향
1. **Program.cs 설정**: `builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));` 추가.
2. **ChzzkChatClient에 Shutdown Hook**: `IHostApplicationLifetime.ApplicationStopping`에 등록하여 모든 웹소켓의 `CloseAsync()`를 순차적으로 호출합니다.
3. **BroadcastScribe 긴급 플러시**: `StopAsync()` 또는 `IDisposable.Dispose()` 에서 `_activeStats`의 모든 데이터를 DB에 벌크 인서트합니다.

#### 위험 요소
- `Dispose()` 메서드(ChzzkChatClient L363-366)에서 `DisconnectAsync(uid).Wait()`을 사용하고 있습니다. 이는 **Deadlock 위험**이 있습니다. `async ValueTask DisposeAsync()`로 전환해야 합니다.
- 30초 타임아웃이라도 100채널의 소켓을 순차적으로 닫으면 부족할 수 있습니다. `Task.WhenAll()`로 병렬 종료를 수행해야 합니다.

---

## 4. 종합 위험도 매트릭스

| # | 이슈 | 위험도 | 현재 영향 (1채널) | 100채널 시 영향 | 우선순위 |
|:---:|:---|:---:|:---:|:---:|:---:|
| 1 | 이중 소켓 아키텍처 (Worker + Client 공존) | 🔴 | 중복 이벤트 가능성 | **확정적 장애** | **P0** |
| 2 | DisconnectAsync Race Condition | 🔴 | 간헐적 좀비 소켓 | **메모리 누수 + 연결 고갈** | **P0** |
| 3 | DB Connection Pool 고갈 | 🔴 | 무영향 | **서비스 전면 중단** | **P0** |
| 4 | Thread Pool 기아 | 🔴 | 무영향 | **응답 지연 → 타임아웃** | **P1** |
| 5 | Graceful Shutdown 부재 | 🔴 | 데이터 소실 가능 | **채팅 통계 전량 소실** | **P1** |
| 6 | N+1 쿼리 패턴 | 🟡 | 미미 | **DB 부하 급증** | **P2** |
| 7 | BackgroundService 중첩 실행 | 🟡 | 무영향 | **API Rate Limit 초과** | **P2** |
| 8 | MemoryStream 과다 할당 | 🟡 | 무영향 | **GC Pause 급증** | **P3** |
| 9 | SignalR 프론트 자동 구독 미구현 | 🟡 | 수동 구독으로 동작 | **오버레이 누락** | **P2** |
| 10 | CancellationToken.None 사용 | 🟡 | 무영향 | **종료 시 데이터 손실** | **P2** |

---

## 5. 권장 개선 로드맵

### Phase 1: 긴급 안정화 (P0, 예상 3~5일)
1. **이중 소켓 아키텍처 해소**: `ChzzkChannelWorker`를 제거하고, `ChzzkBackgroundService`가 `IChzzkChatClient`(피닉스) + `IChzzkBotService.EnsureConnectionAsync()`만 사용하도록 리팩토링.
2. **DisconnectAsync Mutex 도입**: `ConcurrentDictionary<string, SemaphoreSlim>`으로 채널별 연결/해제 직렬화.
3. **DB Pool 확장**: MariaDB `max_connections=300`, EF Core `MaxPoolSize=200` 설정. 또는 **프로필 인메모리 캐시** 도입으로 매 채팅마다의 DB 조회를 제거.

### Phase 2: 성능 최적화 (P1~P2, 예상 5~7일)
4. **Graceful Shutdown 구현**: `HostOptions.ShutdownTimeout = 30s` + `IAsyncDisposable` 패턴 + `BroadcastScribe` 긴급 플러시.
5. **Thread Pool 최적화**: `Task.Run()` 대신 `Channel<T>` 기반 생산자-소비자 패턴으로 전환하여 스레드 사용량을 제어.
6. **N+1 쿼리 해소**: `PeriodicMessageWorker`에서 `Include(p => p.PeriodicMessages)` 사용 또는 단일 조인 쿼리로 변환.
7. **BackgroundService 보호**: `SemaphoreSlim(1,1)`으로 중첩 실행 방지.

### Phase 3: 고도화 (P3, 예상 3~5일)
8. **메모리 최적화**: `RecyclableMemoryStreamManager` 도입 + `ArrayPool<byte>` 활용.
9. **SignalR 프로토콜 최적화**: MessagePack 프로토콜 전환 + 프론트 자동 구독 로직 구현.
10. **모니터링 대시보드**: `/api/health` 엔드포인트에 웹소켓 연결 수, DB Pool 사용률, ThreadPool 큐 길이 노출.

---

> **물멍 파트너의 결론**: 현재 1채널 운영에서는 안정적이지만, **100채널로 확장 시 P0 이슈 3건(이중 소켓, Race Condition, DB Pool 고갈)이 확정적 장애를 유발**합니다. Phase 1의 긴급 안정화를 먼저 수행한 후, 점진적으로 Phase 2/3를 적용하시기 바랍니다. 🌊
