# MooldangBot 100명 스트리머 확장성 분석 및 해결 방안

## 개요
현재 MooldangBot 시스템의 소스코드를 깊이 분석하여, **100명의 스트리머를 동시 서비스**할 때 발생할 수 있는 **7대 구조적 병목 지점**과 그에 대한 **단계적 해결 방안**을 제시합니다.

---

## 📦 기술 스택 및 라이브러리 (MIT License Only)

> [!IMPORTANT]
> 모든 신규 도입 라이브러리는 **MIT 라이선스** 준수를 원칙으로 합니다.

### 현재 사용 중인 패키지 (기존)

| 패키지명 | 버전 | 라이선스 | 용도 | 레이어 |
|:---|:---|:---|:---|:---|
| `Polly` | 8.3.0 | BSD-3 ⚠️ | 탄력성 정책 (Retry, CircuitBreaker) | Application |
| `MediatR` | 12.0.1 | Apache-2.0 | CQRS 이벤트 발행 | Application |
| `Mapster` | 7.4.0 | MIT ✅ | 객체 매핑 | Application, Infra |
| `Pomelo.EntityFrameworkCore.MySql` | 9.0.0 | MIT ✅ | MariaDB EF Core Provider | Infrastructure |
| `Dapper` | 2.1.35 | Apache-2.0 | Micro-ORM | Infrastructure |
| `Microsoft.EntityFrameworkCore` | 9.0.0 | MIT ✅ | ORM (DbContext) | Infrastructure |
| `Newtonsoft.Json` | 13.0.3 | MIT ✅ | JSON 직렬화 | 공통 |
| `DotNetEnv` | 3.1.1 | MIT ✅ | .env 파일 로드 | Infrastructure |

> [!NOTE]
> `Polly` (BSD-3), `MediatR` (Apache-2.0), `Dapper` (Apache-2.0)는 MIT가 아니지만, 모두 **상업적 사용이 자유로운 허용적(Permissive) 라이선스**이므로 기존 사용에 문제 없습니다.

---

### 🆕 Phase별 신규 도입 라이브러리

#### Phase 1: 즉시 안정화 (추가 라이브러리 최소화)

| 패키지명 | 버전 (권장) | 라이선스 | 용도 | 레이어 |
|:---|:---|:---|:---|:---|
| `Microsoft.Extensions.Http.Resilience` | 9.x | MIT ✅ | HttpClient에 Timeout, CircuitBreaker, Retry 정책 체인 부착 (.NET 공식 Polly 확장) | Infrastructure |
| — | — | — | `System.Threading.Channels` (BCL 내장, 별도 설치 불필요) | Application |
| — | — | — | `Parallel.ForEachAsync` (.NET 6+ BCL 내장) | Application |

> **Phase 1은 추가 NuGet 패키지 의존성이 거의 없습니다.** `Microsoft.Extensions.Http.Resilience`만 추가하면 기존 `Polly` 수동 설정을 `.NET 공식 파이프라인`으로 통합할 수 있습니다.

#### Phase 2: 구조 고도화

| 패키지명 | 버전 (권장) | 라이선스 | 용도 | 레이어 |
|:---|:---|:---|:---|:---|
| `Websocket.Client` | 5.x | MIT ✅ | `ClientWebSocket`을 래핑한 자동 재연결, 오류 복구 내장 WS 클라이언트. 현재 수동 구현 중인 `PingLoop`, `ReceiveLoop`, `DisconnectAsync` 재연결 로직을 교체 | Infrastructure |
| `Microsoft.Extensions.Logging` | 9.x | MIT ✅ | 구조화 로깅 (이미 암시적 참조 중이나, Phase 2에서 `Serilog` 연동 시 명시적 참조 필요) | 공통 |
| `Serilog.AspNetCore` | 9.x | Apache-2.0 | 구조화 로깅 + 파일/콘솔/Seq/ElasticSearch 동시 출력. 100명 채널의 상태를 실시간 추적하려면 필수 | Api |
| `Serilog.Sinks.Console` | 6.x | Apache-2.0 | Serilog 콘솔 출력 Sink | Api |
| `Serilog.Sinks.File` | 6.x | Apache-2.0 | Serilog 파일 출력 Sink (롤링 로그) | Api |

> [!NOTE]
> `Serilog` 생태계는 Apache-2.0이지만, .NET 진영에서 사실상 표준 구조화 로깅이며 상업적 제약이 없습니다. MIT만 고수하려면 `Microsoft.Extensions.Logging`의 기본 제공자(Console, Debug, EventSource)만 사용해도 됩니다.

#### Phase 3: 수평 확장

| 패키지명 | 버전 (권장) | 라이선스 | 용도 | 레이어 |
|:---|:---|:---|:---|:---|
| `StackExchange.Redis` | 2.x | MIT ✅ | 분산 캐시, 분산 락, SignalR Backplane | Infrastructure |
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | 9.x | MIT ✅ | SignalR Hub를 다중 인스턴스 간 Redis Backplane으로 연결 | Api |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 9.x | MIT ✅ | IDistributedCache 구현체 (Redis) | Infrastructure |
| `EFCore.BulkExtensions` | 8.x | MIT ✅ | Graceful Shutdown 시 남은 채팅 로그 벌크 인서트 (대량 INSERT 최적화) | Infrastructure |

> [!TIP]
> Phase 3에서 메시지 큐를 도입할 경우, **RabbitMQ.Client** (MIT ✅ / Apache-2.0 Dual License)를 권장합니다. Apache Kafka는 운영 부하가 높으므로 100명 규모에서는 RabbitMQ가 적합합니다.

---

### 🔧 물멍의 추가 추천 라이브러리

사용자가 이미 알고 있는 5종 외에, 100명 확장 시나리오에서 유용한 추가 MIT 라이브러리:

| 패키지명 | 라이선스 | 용도 | 추천 Phase |
|:---|:---|:---|:---|
| `EFCore.BulkExtensions` | MIT ✅ | DB 벌크 INSERT/UPDATE (Graceful Shutdown, 채팅 로그 일괄 저장) | Phase 1~2 |
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | MIT ✅ | 다중 인스턴스 SignalR 동기화 (Redis Backplane) | Phase 3 |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | MIT ✅ | IDistributedCache를 Redis로 구현 (IMemoryCache 대체) | Phase 3 |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | MIT ✅ | Docker/K8s 헬스체크 엔드포인트 (`/healthz`) | Phase 1 |
| `AspNetCore.HealthChecks.MySql` | Apache-2.0 | MariaDB/MySQL 헬스체크 (DB 연결 가능 여부 자동 확인) | Phase 1 |
| `BenchmarkDotNet` | MIT ✅ | 성능 측정 (WS 연결 수 vs 메모리, CPU 프로파일링) | Phase 2 (개발용) |

---

### 📋 EF Core DbContext Pooling 적용 방법

현재 `AddDbContext<AppDbContext>()`를 사용 중이나, **DbContext Pooling**으로 교체하면 100개 이상의 동시 Scope에서 DbContext 생성 오버헤드를 크게 줄일 수 있습니다.

```csharp
// Before (현재)
services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connStr, serverVersion));

// After (DbContext Pooling 적용)
services.AddDbContextPool<AppDbContext>(options =>
    options.UseMySql(connStr, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        mysqlOptions.CommandTimeout(10);
    }), 
    poolSize: 128); // 기본 1024, 100명 서비스 기준 128~256 권장
```

> [!WARNING]
> DbContext Pooling 사용 시 `OnConfiguring`에서 상태를 변경하면 안 됩니다. 현재 `AppDbContext`가 이 조건을 충족하는지 확인 후 적용해야 합니다.

---

## 🔍 현재 아키텍처 요약

```
┌──────────────────────────────────────────────────────┐
│              단일 .NET 10 프로세스 (Docker)             │
│                                                      │
│  ┌───────────────────────────────────────────────┐   │
│  │ ChzzkChatClient (Singleton)                   │   │
│  │  └─ ConcurrentDictionary<uid, ClientWebSocket>│   │
│  │     → 채널당 1 WebSocket + ReceiveLoop        │   │
│  │     → 채널당 1 PingLoop (10초 간격)            │   │
│  │     → 이벤트당 Task.Run(Fire-and-Forget)      │   │
│  └───────────────────────────────────────────────┘   │
│                                                      │
│  ┌────────────────────────────────────────────────┐  │
│  │ ChzzkBackgroundService (1분 루프)               │  │
│  │  └─ foreach(profile) 순차 실행                  │  │
│  │     → EnsureConnection()                        │  │
│  │     → IsLiveAsync()                             │  │
│  └────────────────────────────────────────────────┘  │
│                                                      │
│  ┌────────────────────────────────────────────────┐  │
│  │ SystemWatchdogService (1분 루프)                │  │
│  │  └─ foreach(streamer) 순차 실행                 │  │
│  │     → RenewIfNeeded() + Jitter                  │  │
│  │     → EnsureConnection()                        │  │
│  └────────────────────────────────────────────────┘  │
│                                                      │
│  ┌──────────────┐    ┌──────────────┐               │
│  │ EF Core / DB │    │  SignalR Hub  │               │
│  │ (MariaDB)    │    │ (OverlayHub)  │               │
│  └──────────────┘    └──────────────┘               │
└──────────────────────────────────────────────────────┘
```

---

## 🚨 7대 병목 지점 분석

### 병목 1: 단일 프로세스 내 무한 WebSocket 관리

| 항목 | 상세 |
|:---|:---|
| **파일** | [ChzzkChatClient.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/ChzzkChatClient.cs) |
| **문제** | 하나의 `Singleton` 인스턴스가 `ConcurrentDictionary`로 **모든 채널의 WebSocket**을 관리. 100개 채널 = 100개 WS + 100개 ReceiveLoop + 100개 PingLoop = **최소 200개 상시 Task** |
| **영향** | ThreadPool 고갈, GC Pressure 증가, 단일 장애점(SPOF) — 하나의 예외로 전체 서비스 다운 가능 |
| **심각도** | 🔴 **Critical** |
| **해결 라이브러리** | `Websocket.Client` (MIT) — 자동 재연결, 에러 복구, Dispose 관리 내장 |

### 병목 2: 순차 폴링 루프 O(N) 지연

| 항목 | 상세 |
|:---|:---|
| **파일** | [ChzzkBackgroundService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/ChzzkBackgroundService.cs) L36-66 |
| **문제** | `foreach (var profile in profiles)` 루프에서 **순차적으로** `EnsureConnection()` + `IsLiveAsync()` 호출. 100명 × (API 응답 ~1초) = **한 사이클에 100초 이상 소요** → 1분 주기 감시 불가능 |
| **영향** | 방송 시작/종료 감지 지연, 신규 연결 지연, 와치독의 실시간성 상실 |
| **심각도** | 🔴 **Critical** |
| **해결** | `Parallel.ForEachAsync` (BCL 내장, 추가 패키지 불필요) |

### 병목 3: SystemWatchdogService의 순차 토큰 갱신

| 항목 | 상세 |
|:---|:---|
| **파일** | [SystemWatchdogService.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/SystemWatchdogService.cs) L67-87 |
| **문제** | 100명의 토큰 갱신을 순차 실행 + 랜덤 지터(1~5초). 최악의 경우 **100명 × 5초 = 500초 = 약 8분**이 1사이클에 소요 |
| **영향** | 토큰 만료 대응 지연, 세션 끊김 장기 방치 |
| **심각도** | 🟠 **High** |
| **해결** | `Parallel.ForEachAsync` + 토큰 갱신 전용 BackgroundService 분리 |

### 병목 4: 무제한 Task.Run Fire-and-Forget

| 항목 | 상세 |
|:---|:---|
| **파일** | [ChzzkChatClient.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/ChzzkChatClient.cs) L214 및 [ChzzkChannelWorker.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/ChzzkChannelWorker.cs) L204 |
| **문제** | 매 채팅 이벤트마다 `_ = Task.Run(async () => ...)` 실행. 100개 채널에서 초당 50~100개 채팅 발생 시 **초당 수백 개의 Task**가 ThreadPool에 쌓임. 역압(Backpressure) 메커니즘 부재 |
| **영향** | ThreadPool 기아(Starvation), 메모리 누수, 응답 지연 급증 |
| **심각도** | 🔴 **Critical** |
| **해결** | `System.Threading.Channels.Channel<T>` (BCL 내장) — Bounded Queue 역압 처리 |

### 병목 5: DB 연결 풀 포화 (EF Core DbContext)

| 항목 | 상세 |
|:---|:---|
| **파일** | [DependencyInjection.cs (Infra)](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/DependencyInjection.cs) L19-21 |
| **문제** | `AddDbContext<AppDbContext>`는 기본 커넥션 풀 Max=100. Fire-and-Forget Task마다 `CreateScope()` + DB 조회 실행. 100채널 × 동시 채팅 이벤트 → **커넥션 풀 즉시 고갈** |
| **영향** | `MySqlException: Too many connections`, 전체 DB 쿼리 병목, 서비스 마비 |
| **심각도** | 🔴 **Critical** |
| **해결** | `AddDbContextPool<AppDbContext>()` (EF Core 내장) + 커넥션 스트링 `Max Pool Size=200` |

### 병목 6: 중복 아키텍처 (레거시 + 피닉스 공존)

| 항목 | 상세 |
|:---|:---|
| **파일** | [ChzzkChannelWorker.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Application/Workers/ChzzkChannelWorker.cs) (레거시) vs [ChzzkChatClient.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Infrastructure/ApiClients/Philosophy/ChzzkChatClient.cs) (피닉스) |
| **문제** | 동일한 역할(WS 연결, 채팅 파싱, 이벤트 발행)을 수행하는 **두 개의 구현체**가 공존. DI에서는 피닉스만 등록되어 있지만, 레거시 코드가 331줄로 잔존하여 유지보수 혼란 유발 |
| **영향** | 코드 복잡도 ↑, 버그 유입 리스크, 신입 개발자 온보딩 어려움 |
| **심각도** | 🟡 **Medium** |
| **해결** | `ChzzkChannelWorker.cs` 삭제, 피닉스 일원화 |

### 병목 7: Graceful Shutdown 부재

| 항목 | 상세 |
|:---|:---|
| **파일** | [Program.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.Api/Program.cs) — `HostOptions.ShutdownTimeout` 설정 없음 |
| **문제** | 배포 시 기본 5초 타임아웃으로 강제 종료. 100개 WS 연결의 정리(`CloseAsync`)와 버퍼에 쌓인 채팅 로그의 DB 벌크 인서트가 제한 시간 안에 **완료 불가** |
| **영향** | `TaskCanceledException`, 데이터 유실, 재배포 시 좀비 커넥션 폭발 |
| **심각도** | 🟠 **High** |
| **해결** | `HostOptions.ShutdownTimeout = 30s` + `EFCore.BulkExtensions` (MIT) 벌크 인서트 |

---

## 📐 해결 방안: 3단계 로드맵

### Phase 1: 🛡️ 즉시 안정화 (1~2일, 현 구조 유지)

> 현재 아키텍처를 크게 바꾸지 않고, **즉시 적용 가능한 보강**만 수행합니다.

**필요 NuGet 패키지:**
```xml
<!-- Phase 1: 신규 추가 -->
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="9.*" />
```

#### 1-1. 순차 루프 → 병렬 배치 처리로 전환

```csharp
// Before: 순차 O(N) → 100명 × 1초 = 100초
foreach (var profile in profiles) 
    await botService.EnsureConnectionAsync(profile.ChzzkUid);

// After: 병렬 O(N/M) → 100명 / 10병렬 = ~10초
await Parallel.ForEachAsync(profiles, 
    new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = stoppingToken },
    async (profile, ct) =>
    {
        using var scope = serviceProvider.CreateScope();
        var botSvc = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();
        await botSvc.EnsureConnectionAsync(profile.ChzzkUid);
    });
```

#### 1-2. Task.Run → Channel\<T\> 기반 역압 처리

```csharp
private readonly Channel<ChatEventItem> _eventChannel = 
    Channel.CreateBounded<ChatEventItem>(new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    });

// Consumer: 별도 BackgroundService에서 다중 소비자로 소비
await foreach (var item in _eventChannel.Reader.ReadAllAsync(ct))
{
    using var scope = _scopeFactory.CreateScope();
    await ProcessEventAsync(item, scope, ct);
}
```

#### 1-3. Graceful Shutdown 설정

```csharp
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

#### 1-4. DB 커넥션 풀 확장 → DbContext Pooling 전환

```csharp
// AddDbContext → AddDbContextPool 전환
services.AddDbContextPool<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        mysqlOptions.CommandTimeout(10);
    }), 
    poolSize: 128);

// 커넥션 스트링에 추가: ;Max Pool Size=200;Connection Timeout=10
```

#### 1-5. 헬스체크 엔드포인트 추가

```csharp
// Program.cs
builder.Services.AddHealthChecks();
app.MapHealthChecks("/healthz");
```

---

### Phase 2: 🏗️ 구조 고도화 (3~5일)

**필요 NuGet 패키지:**
```xml
<!-- Phase 2: 신규 추가 -->
<PackageReference Include="Websocket.Client" Version="5.*" />
<PackageReference Include="EFCore.BulkExtensions" Version="8.*" />
<!-- 선택사항: 구조화 로깅 -->
<PackageReference Include="Serilog.AspNetCore" Version="9.*" />
```

#### 2-1. WebSocket 매니저 → Websocket.Client로 교체

현재 수동 구현 중인 `PingLoopAsync`, `ReceiveLoopAsync`, 재연결 로직을 `Websocket.Client`의 내장 기능으로 대체합니다.

```csharp
using Websocket.Client;

// Websocket.Client는 자동 재연결, 에러 복구, 메시지 스트림을 기본 제공합니다.
var client = new WebsocketClient(new Uri(socketUrl));
client.ReconnectTimeout = TimeSpan.FromSeconds(30);
client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);

client.MessageReceived.Subscribe(msg => 
{
    // Channel<T>에 큐잉
    _eventChannel.Writer.TryWrite(new ChatEventItem(chzzkUid, msg.Text));
});

await client.Start();
```

#### 2-2. 레거시 코드 완전 제거

`ChzzkChannelWorker.cs`를 삭제하고, 피닉스(`ChzzkChatClient`)를 `Websocket.Client` 기반으로 완전 리팩토링합니다.

#### 2-3. WebSocket 매니저 세그먼트화

```csharp
public class ShardedWebSocketManager : IChzzkChatClient
{
    private readonly IWebSocketShard[] _shards;
    
    public ShardedWebSocketManager(int shardCount = 10)
    {
        _shards = Enumerable.Range(0, shardCount)
            .Select(_ => new WebSocketShard())
            .ToArray();
    }
    
    private IWebSocketShard GetShard(string chzzkUid) 
        => _shards[Math.Abs(chzzkUid.GetHashCode()) % _shards.Length];
}
```

#### 2-4. SignalR 그룹 라우팅 강화

```csharp
await hubContext.Clients.Group(chzzkUid.ToLower())
    .SendAsync("ReceiveChatMessage", chatData);
```

#### 2-5. 토큰 갱신 전용 BackgroundService 분리

`SystemWatchdogService`에서 토큰 갱신 로직을 분리하여 독립 서비스로 운영합니다.

---

### Phase 3: 🚀 수평 확장 (장기, 1~2주)

**필요 NuGet 패키지:**
```xml
<!-- Phase 3: 신규 추가 -->
<PackageReference Include="StackExchange.Redis" Version="2.*" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="9.*" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.*" />
```

#### 3-1. 다중 인스턴스 배포 (Horizontal Scaling)

```yaml
services:
  app:
    deploy:
      replicas: 4
    environment:
      - SHARD_INDEX=${SHARD_INDEX}
      - SHARD_COUNT=4
```

#### 3-2. Redis 기반 분산 상태 관리

```csharp
builder.Services.AddSignalR().AddStackExchangeRedis(redisConnStr);
builder.Services.AddStackExchangeRedisCache(options => 
    options.Configuration = redisConnStr);
```

#### 3-3. 메시지 큐 도입 (RabbitMQ)

```xml
<!-- 선택사항 -->
<PackageReference Include="RabbitMQ.Client" Version="7.*" /> <!-- MIT / Apache-2.0 Dual -->
```

---

## 📊 예상 효과 요약

| 단계 | 지원 가능 스트리머 수 | 주요 변경 | 신규 패키지 수 | 위험도 |
|:---|:---|:---|:---|:---|
| **현재** | 5~10명 | - | 0 | - |
| **Phase 1** | 30~50명 | 병렬화, Channel, Shutdown, DbContext Pool | +2 | 🟢 낮음 |
| **Phase 2** | 50~100명 | Websocket.Client, 샤딩, 레거시 제거 | +2~3 | 🟡 보통 |
| **Phase 3** | 100~500명+ | Redis, 다중 인스턴스, MQ | +3~4 | 🔴 높음 |

---

## 📋 전체 NuGet 패키지 갱신 요약

```xml
<!-- ============================================ -->
<!-- Phase 1: 즉시 안정화 (신규 추가) -->
<!-- ============================================ -->
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />         <!-- MIT -->
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="9.*" /> <!-- MIT -->

<!-- ============================================ -->
<!-- Phase 2: 구조 고도화 (신규 추가) -->
<!-- ============================================ -->
<PackageReference Include="Websocket.Client" Version="5.*" />        <!-- MIT -->
<PackageReference Include="EFCore.BulkExtensions" Version="8.*" />   <!-- MIT -->

<!-- ============================================ -->
<!-- Phase 3: 수평 확장 (신규 추가) -->
<!-- ============================================ -->
<PackageReference Include="StackExchange.Redis" Version="2.*" />                               <!-- MIT -->
<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="9.*" />   <!-- MIT -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.*" />   <!-- MIT -->
```

---

## ✅ 검증 방안

### 자동화 테스트
- `dotnet build` 빌드 성공 여부 확인
- 헬스체크 엔드포인트 (`/healthz`) 응답 검증

### 수동 검증
1. Phase 1 적용 후 `docker-compose up`으로 기동
2. 로그에서 `Parallel.ForEachAsync` 기반 병렬 처리 확인
3. 10개 이상의 테스트 채널로 동시 WS 연결 유지 확인
4. 재배포 시 Graceful Shutdown 로그 (`ShutdownTimeout: 30s`) 확인

> [!IMPORTANT]
> Phase 1만 적용해도 현재 5~10명 한계를 **30~50명까지 확장** 가능합니다. Phase 2까지 완료하면 100명 목표 달성이 가능합니다.

---

## 🗳️ 결정이 필요한 사항

1. **어떤 Phase부터 진행**할까요? (Phase 1 즉시 안정화 권장)
2. **레거시 코드**(`ChzzkChannelWorker.cs`) 삭제에 동의하시나요?
3. Phase 3의 **Redis/메시지 큐** 도입 시 추가 인프라 비용이 발생합니다. 이에 대한 의견이 있으신가요?
4. **Serilog** (Apache-2.0) 도입에 동의하시나요, 아니면 `Microsoft.Extensions.Logging` 기본 제공자만 사용할까요?
