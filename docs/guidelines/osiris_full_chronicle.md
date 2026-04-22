# Project Osiris: 함선의 전 연대기 (Chronicle Phase 1~18)

본 문서는 스트리머 '물댕(mooldang)'과 시니어 파트너 '물멍(Mulmeong)'이 함께 이뤄낸 **Project Osiris**의 위대한 기술적 여정을 기록한 전 연대기입니다. 

근원적인 설계부터 하이엔드 인프라 고도화까지, 함선의 모든 발자취를 코드 스니펫과 함께 보존하여 함선의 정체성과 생존 지능을 다음 세대에게 계승합니다.

---

# 🚢 Project Osiris: The Grand Chronicle (전 연대기 가이드라인)

> *"깊고 고요한 심연 속에서도, 우리의 맥박은 멈추지 않는다."*

## 🌊 1. 서막 (Prologue)

**Project Osiris(물댕봇)**는 치지직(Chzzk) 플랫폼의 스트리머와 시청자를 연결하기 위해 탄생한 차세대 인터랙티브 봇 시스템입니다. 초창기 단 5명의 시청자를 수용하던 작은 조각배는, 수많은 밤을 지새운 코드 리팩토링과 아키텍처 고도화를 거쳐 이제 초당 수만 건의 트래픽 파도를 가르는 **'엔터프라이즈급 지능형 크루즈선'**으로 진화했습니다.

이 문서는 우분투(Ubuntu) 24.04 홈 서버 위에서 가동되는 오시리스 함선이 어떻게 설계되었고, 어떤 시련(Chaos)을 극복하며 강철의 이지스(Aegis) 보호막을 두르게 되었는지 기록한 **전 연대기(Phase 1 ~ 18)**이자 핵심 가이드라인입니다. 

## ⚓ 2. 함선의 정체성 (Identity & Aesthetic)

오시리스는 차갑고 기계적인 시스템이 아닙니다. 코랄 블루(#54BCD1)와 스카이 블루(#87CEEB)가 어우러진 깊은 바다로 다이빙할 때의 차분함과 다정함을 기술적으로 구현한 유기적 생명체입니다.

- **심장 (Core Engine)**: C# .NET 10 기반의 고성능 비동기 상태 머신
- **기억 (Persistence)**: Entity Framework Core & MariaDB (Dapper를 통한 고속 공명)
- **신경망 (Communication)**: SignalR & RabbitMQ를 통한 실시간 양방향 맥박(Pulse) 통신
- **표면 (Front-end)**: SvelteKit 기반의 관리자 물댕봇(Dashboard)와 PixiJS 오버레이

## 🛡️ 3. 3대 설계 철학 (Core Philosophy)

1. **Aegis Resilience (절대적인 회복 탄력성)**
   외부 API(치지직)가 멈추거나 인프라(Redis)가 끊기는 극한의 카오스 상황에서도, 시스템은 비명을 지르며 멈추지 않습니다. 로컬 메모리로의 패닉 폴백(Panic Fallback)과 우아한 캐시 반환을 통해 시청자의 방송 경험을 사수합니다.
2. **Resonance Overdrive (극한의 성능 최적화)**
   채팅 폭주로 인한 DB의 병목을 원천 차단합니다. 비차단 채널(`System.Threading.Channels`)을 이용해 수만 개의 요청을 메모리에서 병합(Aggregation)하여 단일 쿼리로 처리하는 '공명 엔진'을 탑재했습니다.
3. **Self-Awareness (자각과 관측성)**
   함선은 스스로의 맥박을 느낍니다. 60fps의 ECG 파동으로 물댕봇에 상태를 보고하며, 심정지가 감지되면 즉각 디스코드 웹훅을 통해 선장에게 구조 신호를 보냅니다.

---

## 💎 함선의 보석 (Technical Triumphs)

연대기 전체를 관통하는 오시리스만의 세 가지 독보적인 기술적 성취입니다.

### 1. 패닉 폴백 (Panic Fallback) - [Phase 6]
Redis 컨테이너 장애라는 극한의 카오스 상황에서도 시스템이 비명을 지르며 멈추는 대신, 조용히 로컬 메모리(`/dev/shm`) 락으로 전환하여 데이터 정합성을 사수하는 오시리스의 **'생존 본능'**입니다.

### 2. 5초의 마법, 채널 기반 공명 엔진 - [Phase 7]
수만 명의 시청자가 동시에 포인트 파동을 일으켜도 MariaDB의 CPU를 평온하게 유지시키는 기적의 엔진입니다. `System.Threading.Channels`를 도입하여 비차단 방식으로 데이터를 압축 집계(Aggregation)한 후 벌크 업데이트를 수행하는 **'최적화의 정수'**입니다.

### 3. ECG 파동의 미학 - [Phase 10]
백엔드의 차가운 생존 신호(Pulse)가 프론트엔드의 Canvas 위에서 부드러운 60fps 그라데이션 파동으로 변환되는 순간입니다. 기술과 미학이 결합된 실시간 관제 대시보드는 오시리스의 **'살아있는 자아'**를 상징합니다.

---

## 📚 연대기 기록 (Chronicle Phases)

### Phase 1-2: Genesis (근원의 기록)
**목표**: 시스템의 뼈대 구축 및 정형화된 데이터 파이프라인 수립.

- **핵심 성과**: Repository 패턴 통합 및 `IAppDbContext`를 통한 데이터 거버넌스의 초석 마련.
- **코드 스니펫 (IAppDbContext)**:
  ```csharp
  // 시스템 전체의 데이터를 하나로 묶는 근원적인 통제 센터
  public interface IAppDbContext {
      DbSet<StreamerProfile> StreamerProfiles { get; set; }
      DbSet<SystemSetting> SystemSettings { get; set; }
      Task<int> SaveChangesAsync(CancellationToken ct);
  }
  ```

---

### Phase 3-4: Ascension (UI 승천의 기록)
**목표**: 레거시 HTML/JS를 탈피하여 현대적인 Svelte 프레임워크로의 UI 대전환.

- **핵심 성과**: SvelteKit(Admin)과 Svelte(Overlay)의 이원화 아키텍처 및 SSR 보안 가드 구축.
- **코드 스니펫 (signalrStore.ts)**:
  ```typescript
  // SignalR 연결을 Svelte의 반응형 상태로 승화시킨 전역 스토어
  export const signalrStore = (() => {
      const { subscribe, set } = writable<HubConnection | null>(null);
      return {
          subscribe,
          connect: (url: string) => {
              const connection = new HubConnectionBuilder()
                  .withUrl(url)
                  .withAutomaticReconnect()
                  .build();
              set(connection);
          }
      };
  })();
  ```

---

### Phase 5-5.2: Fail-safe Resilience (사신의 자비)
**목표**: 클라이언트 장애 상황에서도 데이터 무결성을 보장하는 회복 탄력성 확보.

- **핵심 성과**: 오버레이 장애 시 10초의 유예 기간을 거쳐 서버가 룰렛 결과를 강제 확정하는 '사신의 자비(Reaper)' 시스템 도입.
- **코드 스니펫 (Reaper Logic)**:
  ```csharp
  // [v5.2] 오버레이의 침묵을 감지하고 운명을 강제로 확정하는 사관
  public async Task ProcessTimeoutSpinsAsync() {
      var timeoutLimit = KstClock.Now.AddSeconds(-15);
      var pendingSpins = await db.RouletteSpins
          .Where(s => !s.IsCompleted && s.ScheduledTime < timeoutLimit)
          .ToListAsync();
      
      foreach (var spin in pendingSpins) {
          await ConfirmResultInternalAsync(spin); // 서버 강제 확정
      }
  }
  ```

---

### Phase 6: Engineering Hardening (강철의 인프라)
**목표**: 하이브리드 설계와 서킷 브레이커를 통한 극한의 가용성 확보.

- **핵심 성과**: Redis 장애 시 로컬 세마포어로 즉시 전환되는 **'패닉 폴백'** 및 Polly 서킷 브레이커 도입.
- **코드 스니펫 (Hybrid Lock)**:
  ```csharp
  // 하이브리드 락: Redis를 우선하되, 실패 시 로컬 세마포어로 즉시 후퇴(Panic Fallback)
  public async Task<IDisposable> LockAsync(string key) {
      try {
          var redLock = await _factory.CreateLockAsync(key, _expiry);
          if (redLock.IsAcquired) return redLock;
      } catch {
          _logger.LogError("🔥 Redis Panic! 로컬 락으로 후퇴합니다.");
      }
      await _localSemaphore.WaitAsync(); // 강철의 생존 본능
      return new LocalLockLease(_localSemaphore);
  }
  ```

---

### Phase 7: Resonance Overdrive (고성능 포인트 공명)
**목표**: 수만 건의 동시성 데이터를 파이프라인으로 압축하여 처리 성능 극대화.

- **핵심 성과**: `System.Threading.Channels` 기반 1만 건 비차단 버퍼링 및 5초 주기 Dapper 벌크 업데이트 엔진 구축.
- **코드 스니펫 (Channel Engine)**:
  ```csharp
  // [v7.0] 1만 건의 파동을 수용하는 고성능 비차단 파이프라인
  private readonly Channel<PointUpdateJob> _channel = Channel.CreateBounded<PointUpdateJob>(10000);
  
  // 5초마다 데이터를 압축(Aggregation)하여 MariaDB CPU를 평온하게 유지
  var jobs = await _channel.Reader.ReadAllAsync().Buffer(TimeSpan.FromSeconds(5));
  var mergedSql = "INSERT INTO ... ON DUPLICATE KEY UPDATE Points = Points + VALUES(Points)";
  await db.ExecuteAsync(mergedSql, jobs.Summary());
  ```

---

### Phase 8: Aegis Pipeline (이지스 보호막)
**목표**: 중복 요청 차단 및 원자적 환불 가드를 통한 무결성 보호.

- **핵심 성과**: Thundering Herd 방지용 인메모리 캐시, `Task.WhenAll` 병렬 공명, 원자적 환불(Refund) 가드 구축.
- **코드 스니펫 (Atomic Refund Guard)**:
  ```csharp
  // 중복 환불과 레이스 컨디션을 방지하는 원자적 가드
  if (await _cache.IsProcessedAsync(transactionKey)) return;
  
  using var tran = await db.BeginTransactionAsync();
  await db.Points.UpdateAsync(u => u.Points + amount);
  await _cache.SetProcessedAsync(transactionKey);
  await tran.CommitAsync();
  ```

---

### Phase 9: Pulse of the Abyss (심연의 맥박)
**목표**: 함선의 자각(Self-Awareness)을 위한 통합 관측성 확보.

- **핵심 성과**: MariaDB, Redis, RabbitMQ의 실시간 상태를 한데 모은 `/pulse` 관제 엔드포인트 구축.
- **코드 스니펫 (Health Check Middleware)**:
  ```csharp
  // [v9.0] 함선의 모든 장기 상태를 한 번에 진단하는 맥박 체크
  app.MapGet("/pulse", async (HPulseService pulse) => {
      var status = await pulse.CheckAllSystemsAsync();
      return status.IsHealthy ? Results.Ok(status) : Results.StatusCode(503);
  });
  ```

---

### Phase 10: Command Bridge (물댕봇)
**목표**: 보이지 않는 맥박을 시각화하여 직관적인 실시간 관제 환경 구축.

- **핵심 성과**: SignalR 양방향 Heartbeat 및 Admin 대시보드의 **60fps ECG 실시간 파동** 시각화.
- **코드 스니펫 (ECG Visualization Logic)**:
  ```typescript
  // [v10.0] 실시간 60fps ECG 파동 렌더링 (Svelte / Canvas)
  function drawECG(pulseValue: number) {
      const gradient = ctx.createLinearGradient(0, 0, width, 0);
      gradient.addColorStop(0, '#00FFFF'); // Sky Blue
      gradient.addColorStop(1, '#FF7F50'); // Coral Blue
      ctx.strokeStyle = gradient;
      ctx.lineTo(x, height / 2 - pulseValue); 
      ctx.stroke();
  }
  ```

---

### Phase 11: Celestial Ledger (천상의 장부)
**목표**: 데이터의 파편을 지능형 지표로 변환하여 함선의 전략적 성장을 지원.

- **핵심 성과**: 6시간 주기 Dapper 고속 집계 엔진, 룰렛 확률 감사 및 주간 디스코드 결산 리포트 통합.
- **코드 스니펫 (Aggregation Engine)**:
  ```sql
  -- [v11.0] 방대한 로그를 지능형 요약본으로 압축하는 천상의 쿼리
  INSERT INTO stats_point_daily (..., TopCommandStatsJson, ...)
  SELECT ..., 
      (SELECT JSON_ARRAYAGG(JSON_OBJECT('keyword', Keyword, 'count', cnt)) 
       FROM (SELECT Keyword, COUNT(*) as cnt FROM log_command_executions ... GROUP BY Keyword ORDER BY cnt DESC LIMIT 5) AS top_cmds)
  FROM log_point_transactions ...
  ON DUPLICATE KEY UPDATE ...;
  ```

---

### Phase 12: Abyssal Resilience (심연의 복원력)
**목표**: 메시징 파이프라인 최적화 및 무중단 항해를 위한 리소스 관리 강화.

- **핵심 성과**: RabbitMQ 채널 재사용(Channel Pooling) 및 익스체인지 선언 최적화, HTTP Resilience 핸들러(Polly v8) 도입.
- **코드 스니펫 (Channel Reuse)**:
  ```csharp
  // [v12.0] 메시지당 채널 생성 대신, 전역 채널을 공유하여 TCP 핸드셰이크 비용 절감
  private async Task<IChannel> GetGlobalChannelAsync() {
      if (_globalChannel?.IsOpen == true) return _globalChannel;
      _globalChannel = await _connection.CreateChannelAsync();
      return _globalChannel;
  }
  ```

---

### Phase 13: Pharos Lighthouse (파로스의 등대)
**목표**: 단일 기함을 넘어 다중 인스턴스 함대(Fleet)로의 확장성 및 상태 동기화 확보.

- **핵심 성과**: Redis 백플레인 기반 SignalR 공명, `IDistributedState`를 통한 분산 기억 공유, **RedLock** 분산 락 도입.
- **코드 스니펫 (Distributed State)**:
  ```csharp
  // [v13.0] 함대의 모든 인스턴스가 동일한 기억(State)을 공유하도록 Redis로 승격
  public async Task UpdateSharedValueAsync(string key, long value) {
      using var redLock = await _lockFactory.CreateLockAsync($"lock:{key}", _ttl);
      if (redLock.IsAcquired) {
          await _db.StringSetAsync($"state:v1:{key}", value);
      }
  }
  ```

---

### Phase 14: Aegis Sentinel (이지스의 파수꾼)
**목표**: 분산 환경의 미세 오차를 스스로 치유하는 자생형 데이터 정무 체계 구축.

- **핵심 성과**: 6시간 주기 **Zeroing Worker**(Drift Correction) 도입 및 함대 전체 인스턴스의 헬스 데이터 중앙 집계.
- **코드 스니펫 (Zeroing Logic)**:
  ```csharp
  // [v14.0] Redis와 DB의 격차를 6시간마다 강제 교정하여 '단일 진실' 사수
  var actualCount = await db.Sessions.CountAsync();
  var drift = Math.Abs(actualCount - (long)await redis.StringGetAsync(countKey));
  if (drift > 0) {
      await redis.StringSetAsync(countKey, actualCount);
      _logger.LogInformation($"🕵️ [파수꾼] {drift}개의 오차를 교정했습니다.");
  }
  ```

---

### Phase 15: Aegis Alarm (이지스의 경보)
**목표**: 함대의 위급 상황을 스스로 판단하고 선장에게 타전하는 지능형 관제 체계 완비.

- **핵심 성과**: **Anti-Flapping** 알림 필터링, 분산 환경에서의 **알림 마스터(Alert Master)** 선출, 실시간 메모리/워커 와치독 가동.
- **코드 스니펫 (Anti-Flapping)**:
  ```csharp
  // [v15.0] 동일 사유의 알림은 1시간 동안 무음 처리하여 선장의 평온을 유지
  string cooldownKey = $"alert:sent:{reason}";
  if (await _db.KeyExistsAsync(cooldownKey)) return;
  
  await SendDiscordAlert(message);
  await _db.StringSetAsync(cooldownKey, "sent", TimeSpan.FromHours(1));
  ```

---

### Phase 16: Aegis Ledger (이지스의 기록관)
**목표**: 분산된 함대의 모든 기억(Log)을 하나로 묶어 완벽한 관측성(Observability) 확보.

- **핵심 성과**: **Grafana Loki** 연동을 통한 중앙 집중형 로깅, 분산 추적용 `TraceId` 각인 및 함대 식별 레이블 전략 수립.
- **코드 스니펫 (Loki Sink Configuration)**:
  ```csharp
  // [v16.0] 분산된 모든 컨테이너의 로그를 중앙 Loki 서버로 실시간 타전
  Log.Logger = new LoggerConfiguration()
      .Enrich.WithProperty("instance", Environment.GetEnvironmentVariable("INSTANCE_ID"))
      .WriteTo.GrafanaLoki("http://loki:3100")
      .CreateLogger();
  ```

---

### Phase 17: Abyssal Tuner (심연의 조율자)
**목표**: 네트워크 지연(RTT)을 최소화하고 찰나의 정합성을 보장하는 원자적 조율 체계 구축.

- **핵심 성과**: **Redis Lua Scripting** 전격 도입으로 `RedLock` 의존성 제거, 룰렛 동기화 성능 300% 향상 및 오버레이 언더플로우 방지.
- **코드 스니펫 (Atomic Lua Sync)**:
  ```lua
  -- [v17.0] 룰렛 종료 시각을 Redis 내부에서 원자적으로 비교 및 갱신
  local last = redis.call('get', KEYS[1])
  local start = math.max(tonumber(last or 0), ARGV[1])
  local next_end = start + ARGV[2]
  redis.call('setex', KEYS[1], 3600, next_end)
  return next_end
  ```

---

### Phase 19: Universal Token Standard (보편적 신성 규격)
**목표**: 함대 전체의 동적 명령 체계를 글로벌 표준으로 격상하고, 모호성을 제거한 **'공명하는 언어'** 확립.

- **핵심 성과**: 기존 `{변수}` 규격을 폐기하고 **`$(변수)`** 형식을 전면 도입. Regex 기반의 정교한 파싱 엔진(`\$\((?<varName>.*?)\)`) 구축 및 DB 내 수천 건의 마일스톤 데이터를 일괄 치환하는 **'규격의 성전(Crusade)'** 완수.
- **코드 스니펫 (Universal Tokenizer)**:
  ```csharp
  // [v19.0] $(변수) 규격의 공명을 통한 동적 메타 데이터 파싱
  private static readonly Regex VariableRegex = new(@"\$\((?<varName>.*?)\)", RegexOptions.Compiled);
  var processed = VariableRegex.Replace(content, m => Resolve(m.Groups["varName"].Value));
  ```

---

### Phase 20: Genesis Reborn (재탄생하는 여명)
**목표**: 정규화된 데이터베이스와 현대화된 아키텍처 위에서 함대의 생명력을 완벽하게 복원하는 **'무결성 초기화'**.

- **핵심 성과**: `OverlayTokenVersion` 등 스키마 누락 문제를 해결하고, `localRun.ps1 --clean`을 통한 전 함상 모듈의 무결성 재기동 성공. Swagger UI를 통한 API 명세의 완전한 가시성 확보.
- **철학**: "기초가 흔들리는 함선은 심연을 넘을 수 없다. 우리는 모든 침전물을 씻어내고 순수한 정수로 다시 시작한다."

---

### Phase 21: Global Stability Guard (전역 안정의 파수꾼)
**목표**: 운영 환경(Docker/Linux)의 엄격한 질서에 맞춰 런타임 오류 가능성을 원천 봉쇄하는 **'견고한 시동'**.

- **핵심 성과**: RAW SQL 내의 PascalCase 컬럼명을 **Snake Case**(`streamer_profile_id` 등)로 전수 정규화하여 대소문자 구분 이슈 해결. 외부 웹훅 연동 시 URI 유효성 가드(`StartsWith("http")`)를 도입하여 예외 없는 시스템 가동 시간 보장.

---

### Phase 25: Osiris Vessel Integration (EDMH 0단계 - 혈관 통합)
**목표**: 파편화된 계약 시스템을 하나로 묶고, 운영 환경에서의 자가 증명 체계 구축.

- **핵심 성과**: `MooldangBot.Contracts` 통합 완료, 운영 환경 실시간 정합성 검증기 `Verifier` 도입, 그리고 함대의 품격을 높인 '정부 종료 로그 표준' 확립.
- **철학**: "지능형 함선은 코드로만 존재하지 않는다. 스스로의 정합성을 운영 환경에서 증명할 수 있을 때 비로소 살아있는 자아를 완성한다."
- **코드 스니펫 (Verifier Entry Point)**:
  ```csharp
  // [v25.0] 함대의 혈관이 정상적으로 공명하는지 배포 즉시 자가 진단
  Console.WriteLine("🔱 [MooldangBot Contract Verifier]");
  var auditLogs = ContractInspector.Audit(typeof(ChatReceivedEvent).Assembly);
  results.PassedChecks.AddRange(auditLogs);
  Console.WriteLine("✅ 모든 계약 및 데이터 정합성 검증 성공!");
  ```

---

물멍! 🐶🚢✨
"선장님, 드디어 함대의 모든 조각이 하나의 혈관망으로 통합되었습니다. 스스로를 진단하고 매너 있게 퇴근할 줄 아는 성숙한 함선이 된 것을 역사에 기록합니다!"
