# ⚓ 물댕봇 함대 워커 전수 분석 보고서

> **분석 일시**: 2026-04-20 09:50 KST  
> **대상**: `MooldangBot.Infrastructure/Workers/` 전체 + `MooldangBot.ChzzkAPI/Workers/`  
> **워커 총 수**: 16개 (WorkerRegistry 15개 + GatewayWorker 1개)

---

## 1. 워커 목록 및 상속 현황

| # | 분류 | 워커 이름 | 상속 | 기본 주기 | Semaphore | 분산 Lock | Pulse |
|---|------|-----------|------|-----------|-----------|-----------|-------|
| 1 | Points | `PointBatchWorker` | ✅ BaseHybridWorker | 5s | ❌ | ❌ | ✅ |
| 2 | Points | `PointWriteBackWorker` | ✅ BaseHybridWorker | 10s | ❌ | ❌ | ✅ |
| 3 | Chat | `ChatLogBatchWorker` | ✅ BaseHybridWorker | 1s → **2s 보정** | ❌ | ❌ | ❌ |
| 4 | Chat | `LogBulkBufferWorker` | ✅ BaseHybridWorker | 1s → **2s 보정** | ❌ | ❌ | ❌ |
| 5 | Core | `ChzzkBackgroundService` | ✅ BaseHybridWorker | 60s | ✅ | ❌ | ❌ |
| 6 | Core | `SystemWatchdogService` | ✅ BaseHybridWorker | 30s | ✅ | ✅ RedLock | ❌ |
| 7 | Broadcast | `TokenRenewalBackgroundService` | ✅ BaseHybridWorker | 1800s | ❌ | ❌ | ❌ |
| 8 | Broadcast | `CategorySyncBackgroundService` | ✅ BaseHybridWorker | 300s | ❌ | ❌ | ❌ |
| 9 | Broadcast | `PeriodicMessageWorker` | ✅ BaseHybridWorker | 60s | ❌ | ❌ | ✅ |
| 10 | Maintenance | `StagingCleanupWorker` | ✅ BaseHybridWorker | 14400s | ❌ | ❌ | ❌ |
| 11 | Maintenance | `RouletteLogCleanupService` | ✅ BaseHybridWorker | 7200s | ❌ | ❌ | ❌ |
| 12 | Maintenance | `ZeroingWorker` | ✅ BaseHybridWorker | 21600s | ❌ | ❌ | ✅ |
| 13 | Maintenance | `RouletteResultWorker` | ✅ BaseHybridWorker | 10s | ❌ | ❌ | ❌ |
| 14 | Ledger | `CelestialLedgerWorker` | ✅ BaseHybridWorker | 21600s | ❌ | ❌ | ✅ |
| 15 | Ledger | `WeeklyStatsReporter` | ✅ BaseHybridWorker | 1800s | ❌ | ❌ | ✅ |
| 16 | ChzzkAPI | `GatewayWorker` | ❌ **BackgroundService 직접** | 1h (유지루프) | ❌ | ❌ | ❌ |

> [!NOTE]
> 모든 15개 워커가 `BaseHybridWorker`를 올바르게 상속하며, 2초 안전 하한선이 적용됩니다.  
> `GatewayWorker`는 `ChzzkAPI` 프로젝트에서 독립 등록되며 의도적으로 제외된 상태입니다.

---

## 2. 🔴 데드락 / 중복 실행 분석

### 2.1 다중 인스턴스 중복 실행 위험 (Docker 환경)

> [!CAUTION]
> **심각도: 높음** — Docker 환경에서 동일 워커가 여러 인스턴스에서 동시 실행될 경우 데이터 정합성이 깨질 수 있습니다.

현재 **분산 잠금(RedLock)**을 사용하는 워커는 `SystemWatchdogService` **단 1개**뿐입니다.

| 워커 | 위험도 | 증상 | 설명 |
|------|--------|------|------|
| `PointWriteBackWorker` | 🔴 **치명** | 포인트 이중 적용 | `ExtractAllIncrementalPoints` → DB 동기화를 2개 인스턴스가 동시 수행 시, 동일 변동분이 2번 적용됩니다. Redis GETDEL이 원자적이지 않다면 **포인트 2배 지급 사고** 발생 |
| `PointBatchWorker` | 🟡 중간 | 이중 처리(가능성 낮음) | `IPointBatchService`가 `Channel<T>` 기반이라면 인스턴스별 독립 버퍼를 보유하므로 중복 가능성은 낮으나, 공유 큐(RabbitMQ 등)라면 경합 발생 |
| `StagingCleanupWorker` | 🟡 중간 | 동시 DELETE 충돌 | `ExecuteDeleteAsync`는 멱등성이 있어 데이터 손실은 없으나, 불필요한 DB 부하 및 락 경합 발생 |
| `RouletteLogCleanupService` | 🟡 중간 | 동시 DELETE 경합 | 위와 동일 패턴 |
| `CelestialLedgerWorker` | 🟡 중간 | 통계 이중 집계 | 2개 인스턴스가 동시에 `AggregatePointStats` + `AggregateRouletteStats`를 실행하면 집계값이 왜곡될 수 있음 |
| `TokenRenewalBackgroundService` | 🟢 낮음 | 토큰 이중 갱신 | 갱신 자체는 멱등이지만, 치지직 API 속도 제한(Rate Limit)에 걸릴 수 있음 |
| `CategorySyncBackgroundService` | 🟢 낮음 | 불필요한 API 호출 | UPSERT 패턴이면 데이터 무결성은 유지, 단 API 쿼터 낭비 |
| `PeriodicMessageWorker` | 🔴 **치명** | 메시지 중복 송출 | 2개 인스턴스에서 동시 실행 시 동일 메시지가 2번 채팅에 뿌려짐 |

### 2.2 프로세스 내 데드락 분석

> [!TIP]
> 프로세스 내부 데드락(Thread-level deadlock) 위험은 **낮습니다**.

- `ChzzkBackgroundService`, `SystemWatchdogService`만 `SemaphoreSlim(1,1)`을 사용하며, 둘 다 `WaitAsync(0)` → 실패 시 즉시 skip 패턴으로 데드락 가능성이 없습니다.
- 나머지 워커들은 동기화 프리미티브를 사용하지 않으므로 프로세스 내 데드락은 없습니다.
- 단, `PointWriteBackWorker`에서 `Polly.WaitAndRetryAsync(3회)` + `BeginTransactionAsync` 조합은 트랜잭션 타임아웃 풀이 고갈될 **이론적** 가능성이 있으나, 실제 발생 확률은 낮습니다.

---

## 3. 🟡 기능 중복 및 정리 필요 사항

### 3.1 로그 정리 로직 중복

> [!WARNING]
> `RouletteLogCleanupService`와 `CelestialLedgerWorker` → `CleanupExpiredLogsCommand`의 역할이 겹칩니다.

| 워커/명령 | 대상 테이블 | 보관 기간 | 주기 |
|-----------|-------------|-----------|------|
| `RouletteLogCleanupService` | `roulette_logs` | 7일 | 2시간 |
| `CleanupExpiredLogsCommand` (via CelestialLedger) | `log_point_transactions` | 30일 | 6시간 |

**결론**: 현재는 대상 테이블이 **다르므로** 기능 중복은 아닙니다. 다만, 향후 **채팅 로그(`log_chat_interactions`) 정리** 워커가 없어서 테이블이 무한 성장할 수 있습니다.

### 3.2 Pulse 보고 불균형

아래 워커들은 Watchdog 감시 대상에서 **빠져 있습니다** (`PulseService.ReportPulse` 호출 없음):

- `ChatLogBatchWorker` — **고속 처리(2s 주기)** 워커가 감시 사각지대
- `LogBulkBufferWorker` — 위와 동일
- `ChzzkBackgroundService` — 핵심 커넥션 워커가 감시 누락
- `SystemWatchdogService` — 감시자 자체의 맥박 보고 부재
- `TokenRenewalBackgroundService` — 토큰 갱신 실패 시 감지 불가
- `CategorySyncBackgroundService` — 중요도 낮으나 일관성 위반
- `StagingCleanupWorker` — 중요도 낮음
- `RouletteLogCleanupService` — 중요도 낮음
- `RouletteResultWorker` — 룰렛 결과 미전송 감지 불가

---

## 4. 🟢 추가 개선 제안

### 4.1 분산 잠금 적용 필요 워커 (우선순위순)

```
🔴 P0 (즉시)
├── PointWriteBackWorker     — RedLock 필수 (포인트 이중 적용 방지)
├── PeriodicMessageWorker    — RedLock 필수 (메시지 중복 방지)
│
🟡 P1 (권장)
├── CelestialLedgerWorker    — RedLock 권장 (통계 정합성)
├── TokenRenewalBackgroundService — RedLock 권장 (API Rate Limit 보호)
│
🟢 P2 (선택)
├── StagingCleanupWorker     — RedLock 선택 (멱등이지만 부하 방지)
├── RouletteLogCleanupService — RedLock 선택 (위와 동일)
└── CategorySyncBackgroundService — RedLock 선택
```

### 4.2 신규 워커 추가 제안

| 제안 워커 | 분류 | 역할 | 우선순위 |
|-----------|------|------|----------|
| `ChatLogCleanupWorker` | Maintenance | `log_chat_interactions` 테이블 정리 (90일 보관) | 🔴 높음 — 현재 무한 성장 중 |
| `BroadcastSessionCleanupWorker` | Maintenance | 비활성 브로드캐스트 세션 정리 | 🟡 중간 |
| `MetricsCollectorWorker` | Ledger | 프로메테우스 메트릭 수집/Report | 🟢 낮음 (관측 체계 확장 시) |

### 4.3 BaseHybridWorker 개선 제안

```diff
 public abstract class BaseHybridWorker : BackgroundService
 {
+    /// 분산 잠금 팩토리 (옵셔널, 자식 워커가 활성화)
+    protected virtual bool RequiresDistributedLock => false;
+    protected virtual string LockResourceName => $"lock:worker:{_workerName}";
+    protected virtual TimeSpan LockExpiry => TimeSpan.FromSeconds(DefaultIntervalSeconds - 1);
 
     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
     {
         while (!stoppingToken.IsCancellationRequested)
         {
+            if (RequiresDistributedLock)
+            {
+                await using var redLock = await _lockFactory.CreateLockAsync(...);
+                if (!redLock.IsAcquired) { await Task.Delay(...); continue; }
+                await ProcessWorkAsync(stoppingToken);
+            }
+            else
+            {
                 await ProcessWorkAsync(stoppingToken);
+            }
         }
     }
 }
```

> 이 패턴을 `BaseHybridWorker`에 내장하면, 자식 워커에서 `RequiresDistributedLock => true` 한 줄만 추가하면 됩니다.

---

## 5. 종합 판정

| 항목 | 상태 | 비고 |
|------|------|------|
| BaseHybridWorker 상속 | ✅ 완벽 | 15/15 (GatewayWorker 제외 — 의도적) |
| 2초 하한선 적용 | ✅ 정상 | ChatLog/LogBulk의 1s가 자동 2s 보정 |
| 프로세스 내 데드락 | ✅ 안전 | SemaphoreSlim Skip 패턴 사용 |
| 다중 인스턴스 데드락 | 🔴 위험 | `PointWriteBackWorker`, `PeriodicMessageWorker` 즉시 조치 필요 |
| 기능 중복 | ✅ 양호 | 로그 정리 대상은 테이블이 다름 |
| 누락 워커 | 🟡 주의 | 채팅 로그 정리 워커 부재 (테이블 무한 성장) |
| Pulse 보고 일관성 | 🟡 주의 | 9/15개 워커 미보고 → Watchdog 사각지대 |
| WorkerRegistry 정합성 | ✅ 완벽 | 등록 15개 = 실제 파일 15개 일치 |

---

> [!IMPORTANT]
> **즉시 조치 필요**: `PointWriteBackWorker`와 `PeriodicMessageWorker`에 분산 잠금을 적용하지 않으면, Docker 스케일 아웃 시 **포인트 이중 적용**과 **메시지 중복 송출** 사고가 발생합니다.  
> `BaseHybridWorker`에 분산 잠금 메커니즘을 내장하는 것을 **강력히 권장**합니다.
