# [Project Osiris]: 03. 아키텍처 규칙 (Architecture Rules)

본 문서는 오시리스 함선의 구조적 정합성과 유지보수성을 보장하기 위한 설계 규칙을 정의합니다. 모든 모듈은 이 규칙에 따라 상호작용해야 합니다.

---

## 🏗️ 1. 레이어드 아키텍처 (Layered Architecture)

오시리스는 **19개에서 9개 프로젝트**로 정예화되어, 인지 부하를 줄이면서도 10k TPS 수준의 고성능을 보장하는 구조를 가집니다.

1.  **Domain**: 함선의 비즈니스 로직과 전역 공용 명세(Core Specification). 프로젝트의 최하단에서 모든 레이어의 기준이 됩니다.
2.  **Application**: 유스케이스 구현 및 **통합된 컨트롤러/허브**. 비즈니스 로직과 API 진입점이 한곳에 있어 개발 속도가 극대화됩니다.
3.  **Infrastructure**: 데이터베이스 구현 및 인프라 통합 계층. 모든 Background Worker는 **`WorkerRegistry.cs`**를 통해 중앙에서 수명 주기가 관리되며, `appsettings.json`과 연동되어 동적으로 제어됩니다.
4.  **Modules**: 핵심 비즈니스 기능(Commands, Roulette, Point, SongBook)을 독립된 프로젝트로 유지합니다. 각 모듈은 자신만의 `Abstractions`, `Handlers`, `Domain` 요소(기능 전용)를 가지며, 외부 인터페이스(Contracts) 의존을 최소화합니다.

---

## 🚀 2. 10k TPS 대응 데이터 흐름

대형 스트리머의 폭발적인 트래픽에도 안정성을 유지하기 위해 **비동기 배치(Async Batching)** 패턴을 사용합니다.

- **BoundedChannel<T>**: 메시지를 즉시 DB에 쓰지 않고 메모리 큐(Channel)에 담아 부하를 완충합니다.
- **BatchWorker**: 초당 수만 건의 데이터를 `Bulk Insert`나 `MySqlBulkCopy`로 일괄 처리하여 DB I/O 소모를 최소화합니다.

---

## 🛡️ 3. 심연의 복원력: 익산 보험 (Iksan Insurance)

시스템이 예기치 않게 종료되는 상황에서도 시청자의 포인트나 로그 정보가 유실되지 않도록 보장합니다.

- **StopAsync 덤프**: 앱 종료 신호 감지 시, 메모리에 남아있는 버퍼 데이터를 로컬 JSON 파일로 즉시 사수합니다.
- **기동 시 복구**: 시스템 재시작 시 덤프 파일을 읽어 들여 유실된 데이터를 복원한 후 DB에 반영합니다.

---

## 🔒 4. 분산 동시성 및 상태 관리

다중 인스턴스 환경에서 단일 진실 공급원(SSOT)을 유지하기 위해 Redis를 필수 활용합니다.

- **RedLock**: 여러 서버가 동일한 시청자의 포인트를 동시에 수정하려 할 때 데이터 정합성을 보호합니다.
- **Redis Backplane**: 특정 서버로 들어온 이벤트가 함대 전역(모든 서버에 접속한 오버레이)으로 공명하도록 SignalR 백플레인을 가동합니다.

### 🧱 이벤트 발행 및 핸들러 패턴
비송인(Sender)은 이벤트를 던지기만 하고, 이를 누가 처리할지는 관심이 없습니다.

**[핵심 코드: MediatR Handler]**
```csharp
// 1. 이벤트 발행 (Service 레이어)
await _mediator.Publish(new RouletteSpinResultNotification(chzzkUid, spinId, response), ct);

// 2. 비동기 핸들러 (NotificationHandler)
public class RouletteNotificationHandler : INotificationHandler<RouletteSpinResultNotification> {
    public async Task Handle(RouletteSpinResultNotification n, CancellationToken ct) {
        // 알림 전용 로직 수행 (SignalR 전송 등)
        await _hubContext.Clients.Group(n.ChzzkUid).SendAsync("OnRouletteResult", n.Response);
    }
}
```

---

## ⚖️ 3. 의존성 주입(DI) 생명주기 관리

함선의 자원은 유한하며, 잘못된 생명주기 관리는 메모리 누수나 상태 불일치를 초래합니다.

### 🧱 Scoped vs Singleton
- **Singleton**: 전역 상태 관리(`State` 클래스)나 캐시 서비스에 사용합니다.
- **Scoped**: 데이터베이스 컨텍스트(`AppDbContext`)나 비즈니스 서비스(`Service`)에 사용합니다.
- **Transient**: 가벼운 유틸리티나 상태가 없는 계산기에 사용합니다.

**[핵심 코드: DI Configuration]**
```csharp
// 함선의 중앙 관제소(Program.cs)에서의 설정 예시
builder.Services.AddSingleton<RouletteState>(); // [v10.0] 전역 상태 보존
builder.Services.AddScoped<IRouletteService, RouletteService>(); // [v6.2] 요청 단위 상태 격리
builder.Services.AddHostedService<CelestialLedgerWorker>(); // [v11.0] 백그라운드 상주
```

---

## 🔄 4. 백그라운드 서비스 (Background Services)

시간이 오래 걸리거나 주기적으로 실행되어야 하는 작업은 `IHostedService`를 통해 백그라운드에서 조용히 처리합니다.

**[핵심 코드: CelestialLedgerWorker]**
```csharp
// [오시리스의 지능]: 장기 사용되지 않는 리소스(Semaphore 등)는 타이머를 통해 자동 수거합니다.
private void CleanupZombieLocks(...) {
    foreach (var kvp in _localLocks) {
        if (kvp.Value.IsUnused && kvp.Value.IsExpired) {
            _localLocks.TryRemove(kvp.Key, out var entry);
            entry.Dispose(); // OS 핸들 즉시 반환
        }
    }
}
```

---

## 🔒 5. 분산 동시성 제어 (Distributed Concurrency)

오시리스 함대가 다중 인스턴스로 확장됨에 따라, 단일 메모리 락(Semaphore) 대신 **RedLock**을 통한 분산 락을 필수적으로 사용합니다.

### 🧱 RedLock 사용 원칙
- **상태 수정 전용**: 데이터 정합성이 보장되어야 하는 모든 쓰기 작업에 적용합니다.
- **최단 시간 점유**: 락 점유 시간은 10초 이내로 제한하며, 작업 완료 후 즉시 해제합니다.

**[핵심 코드: RedLock Implementation]**
```csharp
using (var redLock = await _lockFactory.CreateLockAsync("lock:resource:id", TimeSpan.FromSeconds(10))) {
    if (redLock.IsAcquired) {
        // 🛡️ [오시리스의 보호]: 단 한 대의 인스턴스만 이 로직을 실행합니다.
        await DoAtomicWorkAsync();
    }
}
```

---

## 🧠 6. 분산 상태 관리 (Distributed State)

함대의 모든 인스턴스는 동일한 '기억(State)'을 공유해야 합니다. `IDistributedState` 인터페이스를 통해 Redis를 단일 진실 공급원(Single Source of Truth)으로 사용합니다.

### 🧱 상태 공유 원칙
- **로컬 캐시 금지**: 룰렛 타이머, 접속자 수 등 공유가 필요한 상태는 절대 `ConcurrentDictionary` 등 로컬 메모리에 저장하지 않습니다.
- **원자적 연산**: 단순 카운트 증감은 Redis의 `Incr`, `Decr`를 사용하며, 복잡한 비교 및 조건부 갱신은 반드시 **Lua Script**를 통해 원자성을 보장합니다.

---

## 🛠️ 7. 찰나의 정합성: Lua Scripting

[Phase 17] 대규모 트래픽 환경에서 네트워크 왕복(RTT)을 줄이고 완벽한 원자성을 확보하기 위해 Redis 내부에서 실행되는 Lua 스크립트를 적극 활용합니다.

### 🧱 Lua 스크립트 작성 원칙
- **순수성 보장**: 스크립트 내에서 무작위 값(`math.random`) 사용을 지양하고 외부에서 인자로 전달받습니다.
- **최소 실행 시간**: 무거운 루프를 피하고 O(1) 또는 O(log N) 수준의 작업만 수행하여 Redis 이벤트 루프를 보호합니다.

**[핵심 코드: ILuaScriptProvider Pattern]**
```csharp
// [v17.0] 룰렛 종료 시각을 소수점 단위의 찰나에서도 정확하게 조율
private const string RouletteSyncScript = @"
    local last = redis.call('get', KEYS[1])
    local start = math.max(tonumber(last or 0), ARGV[1])
    local next_end = start + ARGV[2]
    redis.call('setex', KEYS[1], 3600, next_end)
    return next_end";
```

---

## 🛡️ 8. 심연의 복원력: Panic Fallback & Durability

[Phase 18] 인프라가 붕괴되는 극한의 상황에서도 시스템은 '우아하게' 생존해야 합니다.

### 🧱 생존 원칙
- **패닉 폴백 (Panic Fallback)**: Redis 등 핵심 인프라 단절 감지 시, 예외를 던지는 대신 인메모리(Local Memory) 모드로 즉시 전환하여 가용성을 유지합니다.
- **영속성(Durability) 보장**: 전역 상태가 유실될 위기(앱 종료) 시, 메모리의 잔여 데이터를 로컬 파일(`json` 등)로 덤프하고 재기동 시 복구하는 '익산 보험' 패턴을 적용합니다.

**[핵심 코드: Iksan Insurance Pattern]**
```csharp
// [v18.0] 종료 시점에 데이터를 파일로 사수하는 영속성 전략
protected override async Task StopAsync(CancellationToken ct) {
    if (_retryBuffer.IsEmpty) return;
    var json = JsonSerializer.Serialize(_retryBuffer.ToArray());
    await File.WriteAllTextAsync("temp_backup.json", json);
}
```

---

## 📡 9. 함대 전용 SignalR (Redis Backplane)

다중 서버 환경에서 특정 서버에 접속한 시청자에게 메시지를 보내도 다른 서버의 시청자가 수신할 수 있도록 **Redis 백플레인**을 가동합니다.

- **규칙**: 모든 SignalR 허브 호출은 `Groups(chzzkUid)`를 통해 인스턴스 위치에 관계없이 모든 세션에 공명(Resonance)하도록 설계합니다.

---

## ⚖️ 10. 고성능 파라미터 직렬화

GC 부하를 최소화하기 위해 직렬화 옵션은 반드시 정적(Static) 필드로 관리합니다.

**[핵심 코드: Static Serialization Options]**
```csharp
private static readonly JsonSerializerOptions _options = new() {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

---

## ⚔️ 11. 일제 사격(Salvo) 지휘 체계

[Phase 24] 동일 키워드나 후원 금액에 반응하여 여러 병과(노래 신청, 룰렛 등)가 동시에 출동하는 고도화된 지휘 체계입니다.

### 🧱 일제 사격 집행 원칙
- **통합 결제(Max Cost)**: 매칭된 모든 명령어 중 가장 높은 비용을 단 1회만 결제합니다.
- **독립 실행(Resilient Salvo)**: 각 전략(Strategy) 실행 시 `try-catch` 격벽을 설치하여, 특정 부대의 실패가 전체 발사를 중단시키지 않도록 보장합니다.

**[핵심 코드: Salvo Loop Logic]**
```csharp
// [v12.4] 지휘관의 전술 지침: 하나의 불꽃으로 모든 부대를 깨웁니다.
int maxCost = activeCommands.Max(c => c.Cost);
if (await DeductCurrencyAsync(userId, maxCost)) {
    foreach (var cmd in activeCommands) {
        try {
            await _strategyFactory.Get(cmd.FeatureType).ExecuteAsync(cmd, payload);
        } catch (Exception ex) {
            _logger.LogError(ex, "[일제 사격 부분 실패] 부대: {Feature}", cmd.FeatureType);
        }
    }
}
```

---

물멍! 🐶🚢✨
"선장님, 이제 이 아키텍처 규칙이 함선의 거대한 함대를 하나로 묶어주고 있습니다. 선장님은 그저 명령만 내려주세요, 데이터는 제가 빛의 속도로 동기화하겠습니다!"
