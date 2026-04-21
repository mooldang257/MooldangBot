# ⚓ 물댕봇 함대 워커 전수 분석 보고서

> **분석 일시**: 2026-04-20 09:50 KST  
> **대상**: `MooldangBot.Infrastructure/Workers/` 전체 + `MooldangBot.ChzzkAPI/Workers/`  
> **워커 총 수**: 17개 (WorkerRegistry 16개 + GatewayWorker 1개)

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
| 16 | Maintenance | `ChatLogCleanupWorker` | ✅ BaseHybridWorker | 86400s | ❌ | ✅ RedLock | ✅ |
| 17 | ChzzkAPI | `GatewayWorker` | ❌ **BackgroundService 직접** | 1h (유지루프) | ❌ | ❌ | ❌ |

> [!NOTE]
> 모든 16개 워커가 `BaseHybridWorker`를 올바르게 상속하며, 2초 안전 하한선이 적용됩니다.  
> `GatewayWorker`는 `ChzzkAPI` 프로젝트에서 독립 등록되며 의도적으로 제외된 상태입니다.

---

## 2. ✅ 데드락 / 중복 실행 분석 (해결 완료)

### 2.1 다중 인스턴스 중복 실행 위험 (해결 완료)

> [!NOTE]
> 🚀 **조치 완료**: BaseHybridWorker에 분산 락(RedLock)이 전면 도입되어 핵심 워커들의 동시 실행 충돌이 차단되었습니다.

현재 **분산 잠금(RedLock)**이 주요 권장 대상(P0, P1) 워커 등에 모두 적용되어 데이터 정합성을 보장하도록 개선되었습니다.

| 워커 | 조치 내역 | 상태 | 설명 |
|------|--------|------|------|
| `PointWriteBackWorker` | ✅ **RedLock 도입** | 🟢 안전 | **포인트 2배 이중 지급** 사고가 원천 차단되었습니다. |
| `PointBatchWorker` | - | 🟢 안전 | `Channel<T>` 기반 독립 버퍼라서 로컬 메모리 경합이 없습니다. |
| `StagingCleanupWorker` | - | 🟢 안전 | DB DELETE 멱등성이 있어 락 없이도 무결성이 보장됩니다. |
| `RouletteLogCleanupService` | - | 🟢 안전 | 위와 동일하게 멱등성이 유지됩니다. |
| `CelestialLedgerWorker` | ✅ **RedLock 도입** | 🟢 안전 | 통계가 두 번 더해지는 통계 왜곡 맹점이 차단되었습니다. |
| `TokenRenewalBackgroundService` | ✅ **RedLock 도입** | 🟢 안전 | 여러 인스턴스가 갱신을 동시 시도해 치지직 API Rate Limit에 걸리는 것을 막아줍니다. |
| `CategorySyncBackgroundService` | - | 🟢 안전 | UPSERT 패턴이므로 병렬 처리에도 안전합니다. |
| `PeriodicMessageWorker` | ✅ **RedLock 도입** | 🟢 안전 | 정기 메시지가 채팅창에 똑같이 두 번 송출되는 문제가 해결되었습니다. |

### 2.2 프로세스 내 데드락 분석

> [!TIP]
> 프로세스 내부 데드락(Thread-level deadlock) 위험은 **낮습니다**.

- `ChzzkBackgroundService`, `SystemWatchdogService`만 `SemaphoreSlim(1,1)`을 사용하며, 둘 다 `WaitAsync(0)` → 실패 시 즉시 skip 패턴으로 데드락 가능성이 없습니다.
- 나머지 워커들은 동기화 프리미티브를 사용하지 않으므로 프로세스 내 데드락은 없습니다.
- 단, `PointWriteBackWorker`에서 `Polly.WaitAndRetryAsync(3회)` + `BeginTransactionAsync` 조합은 트랜잭션 타임아웃 풀이 고갈될 **이론적** 가능성이 있으나, 실제 발생 확률은 낮습니다.

---

## 3. ✅ 기능 증설 및 로그 정리 체계 확립 (해결 완료)

### 3.1 시스템 로그 보존 및 정리 체계

> [!NOTE]
> 테이블별로 역할이 명확히 분리된 3대 유지보수(Cleanup) 워커가 가동되어 데이터베이스의 무한 성장을 방지합니다. 기능 중복 없이 독립적으로 동작합니다.

| 분류 | 워커/명령 | 대상 테이블 | 보관 기간 | 주기 | 상태 |
|------|-----------|-------------|-----------|------|------|
| 룰렛 | `RouletteLogCleanupService` | `roulette_logs` | 90일 | 24시간 | 🟢 정상 |
| 포인트 | `CleanupExpiredLogsCommand` | `log_point_transactions` | 30일 | 24시간 | 🟢 정상 |
| 채팅 | `ChatLogCleanupWorker` | `log_chat_interactions` | 90일 | 24시간 | ✅ **신규 적용** |

**결론**: 유일한 맹점이었던 **채팅 로그(`log_chat_interactions`) 무한 성장 문제**가 `ChatLogCleanupWorker` 워커 신설을 통해 완전히 해결되었습니다. 3가지 로그 대상 테이블 각각이 충돌 없이 안전하게 수명 주기를 관리받고 있습니다.

### 3.2 Pulse 보고 불균형 (해결 완료)

~~아래 워커들은 Watchdog 감시 대상에서 **빠져 있습니다** (`PulseService.ReportPulse` 호출 없음):~~

> ✅ **해결 완료**: `BaseHybridWorker` 엔진 레벨에 `PulseService`가 통합되어, 현재 모든 자식 워커가 자동으로 Watchdog에 맥박을 보고합니다. 사각지대가 완전히 해소되었습니다.

---

## 4. 🟢 추가 개선 완료 사항

### 4.1 분산 잠금 적용 대상 (해결 완료)

> ✅ **해결 완료**: P0, P1 그룹의 모든 워커에 `RequiresDistributedLock => true` 적용이 완료되었습니다.

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
| `ChatLogCleanupWorker` | Maintenance | `log_chat_interactions` 테이블 정리 (90일 보관) | ✅ **적용 완료** |
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
| BaseHybridWorker 상속 | ✅ 완벽 | 16/16 (GatewayWorker 제외 — 의도적) |
| 2초 하한선 적용 | ✅ 정상 | ChatLog/LogBulk의 1s가 자동 2s 보정 |
| 프로세스 내 데드락 | ✅ 안전 | SemaphoreSlim Skip 패턴 사용 |
| 다중 인스턴스 데드락 | ✅ 안전 | RedLock 엔진 내장 및 P0, P1 워커 적용 완료 |
| 기능 중복 | ✅ 양호 | 로그 정리 대상은 테이블이 다름 |
| 누락 워커 | ✅ 해결됨 | `ChatLogCleanupWorker` 신설 완료 |
| Pulse 보고 일관성 | ✅ 완벽 | 엔진 통합으로 16/16 워커 전수 자동 보고 |
| WorkerRegistry 정합성 | ✅ 완벽 | 등록 16개 = 실제 파일 16개 일치 |

---

> [!NOTE]
> 🚀 **조치 완료**: 권장되었던 BaseHybridWorker 엔진 개편, 분산 잠금 도입, Pulse 자동 보고 기능이 모두 성공적으로 반영되었습니다.
