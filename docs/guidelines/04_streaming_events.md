# [Project Osiris]: 04. 스트리밍 이벤트 및 부하 분산 (Streaming Events)

본 문서는 오시리스 함선이 직면하는 **폭발적인 트래픽과 실시간성**을 보장하기 위한 고도화된 이벤트 처리 아키텍처를 정의합니다.

---

## 📡 1. 실시간 양방향 맥박 (SignalR Heartbeat)

함선과 오버레이 사이의 연결 상태를 실시간으로 감시하여 장애를 즉각 감지합니다.

### 🧱 ECG 차트 및 맥박 체크
오버레이가 죽었을 때 서버가 이를 감지하여 룰렛 결과를 대신 확정하거나, 물댕봇(Admin)에 시각적으로 보고합니다.

**[핵심 코드: SignalR Pulse]**
```csharp
// [v10.0] 물댕봇(Admin) 대시보드에 실시간 ECG 파동을 전송합니다.
public async Task ReportPulse(string overlayToken) {
    _logger.LogDebug($"[맥박 확인] {overlayToken} 생존 신호 수신");
    await Clients.All.SendAsync("OnPulseReceived", overlayToken, DateTime.UtcNow);
}
```

---

## 🔒 2. 하이브리드 락: 패닉 폴백 (Panic Fallback)

분산 환경에서의 정합성과 극한의 상황에서의 가동성을 동시에 보장합니다.

### 🛡️ 설계 철학
- **Primary**: Redis 분산 락 (`RedLock.net`)을 사용하여 멀티 인스턴스 전역 정합성 유지.
- **Fallback**: Redis 장애 시 시스템이 멈추는 대신 로컬 `SemaphoreSlim`으로 즉시 후퇴하여 최소한의 정합성 사수.

**[핵심 코드: Hybrid Lock Logic]**
```csharp
public async Task<IDisposable?> AcquireLockAsync(string key, TimeSpan wait, TimeSpan expiry) {
    try {
        // [v6.2] Redis 분산 락 시도
        var redLock = await _lockFactory.CreateLockAsync(key, expiry, wait, ...);
        if (redLock.IsAcquired) return redLock;
    } catch (Exception ex) {
        // 🔥 Redis Panic! 로컬 메모리 락으로 긴급 후퇴합니다.
        _logger.LogCritical(ex, "🎰 [생존 본능] Redis 장애 감지됨. 로컬 락으로 폴백합니다.");
    }
    
    // 강철의 생존 본능: 로컬 세마포어 대기
    await GetLocalSemaphore(key).WaitAsync(wait);
    return new LocalLockLease(key);
}
```

---

## ⚡ 3. 채널 기반 집계 엔진 (Resonance Engine)

수만 명이 동시에 채팅을 치거나 포인트를 획득할 때 DB 부하를 평온하게 유지하는 핵심 기술입니다.

### 🧱 System.Threading.Channels
폭주하는 요청을 **비차단 버퍼링**하고, 일정 주기마다 메모리에서 데이터를 병합(Aggregation)하여 MariaDB에 단일 쿼리로 밀어 넣습니다.

**[핵심 코드: Point Resonance Overdrive]**
```csharp
// [v7.0] 10,000건의 파동을 수용하는 고성능 비차단 파이프라인
private readonly Channel<PointUpdateJob> _channel = Channel.CreateBounded<PointUpdateJob>(10000);

// [v12.0] 배치 사이징: 최대 2,000건씩 쪼개어 소화 (메모리 보호)
await foreach (var job in _channel.Reader.ReadAllAsync(ct)) {
    jobs.Add(job);
    if (jobs.Count >= 2000) break; 
}

// [v12.0] 원자적 벌크 업데이트 및 ID 배치 매핑 (Batch Fetch)
using var transaction = await db.Database.BeginTransactionAsync(ct);
var viewerMap = await db.GlobalViewers.Where(g => hashes.Contains(g.Hash)).ToDictionaryAsync(...);
await connection.ExecuteAsync(bulkSql, params, transaction); 
await transaction.CommitAsync(ct);
```

---

// [v12.0] 메시징 격리 파이프라인: 외부 시스템(RabbitMQ) 지연이 메인 로직을 방해하지 않도록 격리합니다.
private readonly Channel<EventItem> _msgBuffer = Channel.CreateBounded<EventItem>(5000);

// Worker: 전용 발행 루프 (Non-blocking Wing)
await foreach (var item in _msgBuffer.Reader.ReadAllAsync(ct)) {
    await rabbitMq.PublishAsync(item);
}
```

---

## 🚨 5. Thundering Herd 방지 (Aegis Cache)

동일한 시각자 정보나 스트리머 설정을 동시에 수천 번 조회할 때 DB 성능 저하를 방지합니다.

### 🧱 2단계 캐싱 전략
1.  **L1 (Memory)**: 수 밀리초 단위의 극단적인 가드.
2.  **L2 (Redis)**: 수 초~분 단위의 분산 상태 공유.

**[핵심 코드: Identity Cache]**
```csharp
// [v8.0] 단 1초의 인메모리 캐싱만으로도 폭발적인 동시 조회를 차단합니다.
public async Task<StreamerProfile?> GetProfileCachedAsync(string uid) {
    return await _cache.GetOrCreateAsync($"profile:{uid}", async entry => {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1); // 1초 가드
        return await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.ChzzkUid == uid);
    });
}
```

---

## 💰 4. 치지직 후원 감지 및 해독 (Chzzk Donation Recognition)

치지직의 비공식 필드(`msgTypeCode`)의 불확실성을 배제하고, 공식적인 데이터 구조에 기반한 해독 규칙을 적용합니다.

- **진실의 근원(Source of Truth)**: 오직 **`donationType`** 필드를 통해 후원 여부를 판별합니다.
- **해독 공정 (2-Step Parsing)**: 이벤트 래퍼(`ChatEventItemWrapper`)를 먼저 분석하여 고유 식별자(`CorrelationId`)를 확보한 후, 내부 페이로드를 정밀 분석합니다.
 
---
 
## 🚫 6. 봇 자가 응답 피드백 루프 방지 (Self-Response Protection)
 
봇이 자신의 응답을 다시 명령어나 이벤트로 오인하여 발생하는 무한 루프 및 데이터 오염을 방지합니다.
 
### 🛡️ 설계 철학: 입구 원천 차단 (Gateway-Level Filtering)
- **원칙**: 불필요한 이벤트는 발생지(Source)에서 가장 가까운 입구에서 걸러내어 인프라 자원을 보호해야 합니다.
- **구현**: 게이트웨이(`ChzzkAPI`)의 WebSocket 레이어에서 `BOT_CHZZK_UID`를 대조하여 RabbitMQ 발행 자체를 차단합니다.
 
**[핵심 전략]**
1.  **환경 변수 관리**: `.env` 파일에 `BOT_CHZZK_UID`를 설정하여 하드코딩을 배제합니다.
2.  **부하 분산**: 메시지가 `.app` 서비스까지 도달하지 않으므로 RabbitMQ와 Application 서버의 연쇄적인 부하를 원천 차단합니다.
3.  **데이터 무결성**: 봇의 활동이 `log_chat_interactions`나 포인트 적립 로직에 영향을 주지 않도록 보장합니다.

---

물멍! 🐶🚢✨
"선장님, 오시리스 함선의 4대 기술 헌장이 완성되었습니다. 이 문서들은 거친 스트리밍의 바다에서도 우리 함선을 가장 빠르고 견고하게 지켜줄 것입니다!"
