# 02. C# Style & Clean Code

## 1. 개요
MooldangBot은 .NET 10의 최신 기능을 적극 활용하여 고성능을 유지합니다. 이 문서는 가독성뿐만 아니라 서버 자원(CPU/RAM)을 효율적으로 쓰기 위한 코딩 표준을 정의합니다.

## 2. 비동기 프로그래밍 (Async / Await)
잘못된 비동기 처리는 스레드 풀(Thread Pool)의 고갈이나 예측 불가능한 크래시를 유발합니다. 서버 자원 회수를 위해 `CancellationToken` 전파는 **엄격한 의무(Mandatory)**사항입니다.

❌ **Don't: 예외 처리가 불가능한 `async void`**
```csharp
// [위험] 호출자가 예외를 잡을 수 없으며, 컨테이너를 전체 크래시시킬 수 있습니다.
public async void ProcessEvent() 
{
    await PerformTask();
}
```

❌ **Don't: CancellationToken을 무시한 영속성/네트워크 호출**
```csharp
// [위험] 서버 종료 시그널이 와도 쿼리가 끝날 때까지 스레드를 물고 놓지 않습니다.
var users = await _dbContext.StreamerProfiles.ToListAsync(); 
```

✅ **Do: 모든 비동기 말단에 CancellationToken 전파**
```csharp
// [안전] 연결이 끊어지거나 서버 종료 시 즉각적으로 작업을 취소하여 자원을 회수합니다.
public async Task ProcessEventAsync(CancellationToken ct)
{
    try 
    {
        var users = await _dbContext.StreamerProfiles.ToListAsync(ct);
        await PerformTaskAsync(ct);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("작업이 취소되었습니다.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "이벤트 처리 중 오류 발생");
    }
}
```

## 3. 데이터 모델링 (DTO & Commands)
상태 변화가 없는 데이터 전달 객체는 불변성(Immutability)을 보장해야 합니다.

❌ **Don't: 장황하고 가변적인 Class 기반 DTO**
```csharp
public class ChatMessageDto 
{
    public string Nickname { get; set; }
    public string Content { get; set; }
}
```

✅ **Do: 간결하고 안전한 `record` 활용**
```csharp
// [효율] .NET의 record는 불변성과 값 기반 비교를 기본으로 제공합니다.
public record ChatMessageDto(string Nickname, string Content);
```

## 4. 의존성 주입 및 생성자 (.NET 10)
비대해지는 생성자 코드를 줄이고 가독성을 높입니다.

❌ **Don't: 전통적인 생성자 주입**
```csharp
public class RouletteService 
{
    private readonly IAppDbContext _db;
    public RouletteService(IAppDbContext db) 
    {
        _db = db;
    }
}
```

✅ **Do: 최신 Primary Constructor 문법**
```csharp
// [깔끔] .NET 10 스타일의 기본 생성자로 코드를 압축합니다.
public class RouletteService(IAppDbContext db, ILogger<RouletteService> logger) 
{
    // [아키텍트 팁]: 주입받는 의존성이 5~6개를 넘어간다면 설계상 
    // 해당 클래스가 너무 많은 책임을 지고 있는 것(SRP 위배)일 수 있습니다.
}
```

## 5. 로깅 (Structured Logging)
로그는 단순 문자열이 아니라 분석 가능한 데이터여야 합니다.

❌ **Don't: 보간된 문자열 로깅 (메모리 낭비)**
```csharp
_logger.LogInformation($"Viewer {nickname} joined the room."); 
```

✅ **Do: 구조적 로깅 (Structured Logging)**
```csharp
// [최적화] CPU 부하를 줄이고 로그 서버에서 필터링이 용이해집니다.
_logger.LogInformation("Viewer {Nickname} joined the room.", nickname);
```

---
**최종 승인**: 2026-04-02 (아키텍트 검토 완료)
