# 치지직 채팅 소켓 무중단(Zero-Downtime) 서비스 설계 (v4.1)

mooldang님, 방송 중 채팅이나 후원이 끊기는 상황이 얼마나 큰 스트레스인지 잘 알고 있습니다. 그 불편함을 없애기 위해, 끊김이 발생하더라도 찰나의 순간에 복구하는 **무중단 복원력(Resilience)** 중심의 설계를 제안합니다.

## 💡 복원력(Resilience) 철학
- **현실 인정**: 네트워크 세계에서 "절대 끊어지지 않는 소켓"은 없습니다. 외부 요인은 통제 불가능합니다.
- **목표**: 서버가 우리를 "유령(Idle)"으로 오해해 끊지 않도록 방어하고, 만약 끊기더라도 시청자가 눈치채지 못할 만큼 빛의 속도로 복구하는 것입니다.

## 작업 체크리스트
- [x] 무중단 아키텍처 설계 및 시니어 개발자 철학 반영
- [x] `task.md` 업데이트
- [x] [ChzzkChannelWorker.cs](file:///c:/webapi/MooldangAPI/Services/ChzzkChannelWorker.cs) 무중단 로직 이식
    - [x] 병렬 루프(`Task.WhenAny`) 구조 도입
    - [x] 독립된 `PingLoopAsync` (10s 주기 '심장 박동') 구현
    - [x] 16KB 대용량 수신 버퍼 확장
    - [x] `IServiceScopeFactory` 기반 Scoped DB 처리 유지
- [x] 재연결 딜레이 최소화 (500ms)
- [x] 검증 및 결과 보고

---

## 🛠️ 무중단 아키텍처 상세 설계

### 1. 병렬 루프 제어 (Task.WhenAny)
- **구조**: `ReceiveLoopAsync`(수신)와 `PingLoopAsync`(생존 신고)를 별도 태스크로 동시 실행합니다.
- **감시**: 어느 한 루프라도 에러가 나거나 중단되면 즉시 소켓을 폐기하고 0.5초 안에 재접속 프로세스를 가동합니다.

### 2. 전용 '심장 박동(Heartbeat)' 시스템
- **주기**: **10초**마다 Socket.IO Ping 패킷(`"2"`)을 강제로 송신하여 치지직 서버가 우리를 "산 사람"으로 인식하게 만듭니다. (0%에 가까운 강제 종료 방어)

### 3. 초고속 복구 및 수신 강화
- **딜레이**: 재연결 대기 시간을 **500ms**로 설정하여 메시지 유실 구간을 최소화합니다.
- **버퍼**: 대용량 후원 데이터에 대비해 수신 버퍼를 **16KB**로 확장합니다.

---

## 📋 핵심 구현 가이드 (.NET 10 스타일)

```csharp
// ChzzkChannelWorker.cs - 무중단 병렬 루프 핵심 구조
private async Task ConnectAndListenAsync(string chatChannelId, CancellationToken ct) {
    while (!ct.IsCancellationRequested) {
        using var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        try {
            await ws.ConnectAsync(serverUrl, ct);
            using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            
            // 수신과 핑을 병렬로 가동 (생존 신고와 데이터 수집 분리)
            var receiveTask = ReceiveLoopAsync(ws, loopCts.Token);
            var pingTask = PingLoopAsync(ws, loopCts.Token);

            await Task.WhenAny(receiveTask, pingTask);
            loopCts.Cancel(); 
        } catch (Exception ex) {
            _logger.LogError(ex, "❌ [ChzzkChat] 연결 오류. 즉각 재연결 시도.");
        } finally {
            if (!ct.IsCancellationRequested) await Task.Delay(500, ct); 
        }
    }
}

// 🩺 10초 주기 심장 박동 루프 (v4.1 조정)
private async Task PingLoopAsync(ClientWebSocket ws, CancellationToken ct) {
    while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested) {
        await Task.Delay(TimeSpan.FromSeconds(10), ct); // 10초로 강화
        await ws.SendAsync(pingBuffer, WebSocketMessageType.Text, true, ct);
    }
}
```

---

## 🔍 사후 분석 및 운영 전략
- **에러 모니터링**: 1006(Abnormal Closure) 에러 빈도를 추적하여 환경적 요인을 분석합니다.
- **동시성 보장**: `IServiceScopeFactory`를 통해 각 메시지 처리 시 독립된 DB 생명 주기를 보장합니다.