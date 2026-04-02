# 04. Streaming & SignalR Events

## 1. 개요
MooldangBot의 생동감은 실시간 이벤트 처리에 달려 있습니다. 이 문서는 수많은 시청자 이벤트(채팅, 후원 등)가 발생할 때 시스템의 안정성을 유지하며 오버레이에 정보를 전달하는 규칙을 정의합니다.

## 2. 실시간 이벤트 파이프라인 (Backpressure & Separation)
이벤트는 발생 즉시 처리되는 것이 아니라, **'채널(Channel)'**이라는 완충 지대를 거쳐 소비되어야 합니다. 특히 데이터의 성격에 따라 채널을 분리하여 관리해야 합니다.

✅ **Do: 채널 분리 및 고용량 버퍼 전략**
- **일반 채팅 채널**: 대량 발생하므로 고용량(10,000~50,000건) 버퍼와 `DropOldest` 전략을 사용하여 시스템 부하를 방어합니다.
- **금융/명령어 채널**: 후원이나 포인트 룰렛 등 재화 관련 이벤트는 절대 드롭되어서는 안 됩니다. 소용량 버퍼와 `Wait` 전략을 사용하여 반드시 모든 이벤트를 처리합니다.

```csharp
// [안전] 32GB RAM 인프라를 활용하여 50,000건 수준의 버퍼를 확보합니다.
var chatChannel = Channel.CreateBounded<ChatItem>(new BoundedChannelOptions(50000) 
{ 
    FullMode = BoundedChannelFullMode.DropOldest 
});

// [필수] 재화 관련 채널은 별도로 분리하여 유실을 방지합니다.
var financeChannel = Channel.CreateBounded<FinanceItem>(new BoundedChannelOptions(1000) 
{ 
    FullMode = BoundedChannelFullMode.Wait 
});
```

## 3. SignalR 그룹 관리 (Targeted Broadcasting)
전체 브로드캐스팅(`Clients.All`)은 자제하며, 반드시 **스트리머별 그룹**을 활용합니다. 그룹 명칭은 보안을 위해 노출되지 않는 토큰을 기반으로 관리해야 합니다.

## 4. 오버레이 접속 및 보안 (Critical)
치지직 UID는 공개된 정보이므로, 이를 직접 그룹 가입의 키로 사용해서는 안 됩니다. 악의적인 세션 탈취 및 자원 고갈 공격을 방어하기 위해 **보안 토큰(GUID 등)** 검증이 필수입니다.

❌ **Don't: 공개된 UID 기반의 그룹 가입**
```csharp
// [위험] 누구나 내 스트리밍 이벤트 그룹에 가입하여 데이터를 가로챌 수 있습니다.
var uid = Context.GetHttpContext()?.Request.Query["chzzkUid"];
await Groups.AddToGroupAsync(Context.Id, uid);
```

✅ **Do: 암호화된 토큰 기반의 그룹 가입**
```csharp
public override async Task OnConnectedAsync()
{
    var token = Context.GetHttpContext()?.Request.Query["token"].ToString();
    if (!string.IsNullOrEmpty(token))
    {
        // [보안] 인메모리 캐시나 DB에서 토큰에 해당하는 스트리머 UID를 검증 후 가입
        var streamerUid = _tokenService.GetUidByToken(token);
        if (streamerUid != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, streamerUid);
            _logger.LogInformation("보안 토큰 인증 성공: {StreamerUid}", streamerUid);
        }
    }
    await base.OnConnectedAsync();
}
```

## 5. 이벤트 Throttling (Presentation)
빈번한 UI 갱신은 시청자의 웹 브라우저 부하를 초래합니다. 중요도가 낮은 데이터는 `Application` 레이어에서 취합(Batch)하거나 일정 주기로 전송하는 것을 권장합니다.

---
**최종 승인**: 2026-04-02 (아키텍트 검토 완료 / 'High-Capacity & Token Security' 원칙 적용)
