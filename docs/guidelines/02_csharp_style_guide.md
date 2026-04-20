# [Project Osiris]: 02. C# 스타일 가이드 (Style Guide)

본 문서는 오시리스 함선에서 가동되는 **C# .NET 10**의 표준 스타일과 코딩 규약을 정의합니다. 모든 코드는 가독성, 성능, 안정성을 최우선으로 작성되어야 합니다.

---

## ⚡ 1. .NET 10 최신 문법 활용 (Modern Syntax)

오시리스는 최신 기술의 정점을 지향합니다. 중복 코드를 줄이고 가시성을 높이는 최신 문법을 적극 활용합니다.

### 🏗️ Primary Constructor & required
생성자 주입(DI)과 데이터 필수 여부를 가장 우아하게 표현합니다.

**[핵심 코드: Primary Constructor & required]**
```csharp
// [v10.1] Primary Constructor를 사용한 간결한 DI 주입
public class RouletteService(IAppDbContext db, IMediator mediator) : IRouletteService {
    // 필드 선언 없이 바로 사용 가능
}

// [v11.0] required 프로퍼티를 통한 필수 필드 강제 (Entity 및 DTO 공통)
public class StreamerProfile : ISoftDeletable {
    [Key] public int Id { get; init; } // 식별자는 고정(init)
    [MaxLength(50)] public required string ChzzkUid { get; set; } // 필수값(required)
}
```

### 📦 record 타입 (Immutability)
데이터 전송 객체(DTO)와 이벤트 정의 시 불변성을 보장하는 `record`를 사용합니다.

**[핵심 코드: record]**
```csharp
// 비즈니스 로직과 데이터 레이어의 비결합을 위한 레코드 정의
public record RouletteSpinCompletedEvent(
    string ChzzkUid, 
    int SpinId, 
    List<RouletteItem> Results
) : INotification;
```

---

## 📡 2. 비동기 처리 표준 (Async Standards)

방송 이벤트는 폭발적으로 발생하므로 비동기 처리는 함선의 생존과 직결됩니다.

### 🧱 ValueTask & Task
- 결과값이 이미 캐시되어 있거나 빠르게 반환되는 경우 `ValueTask`를 사용하여 할당 부하를 최소화합니다.
- 복잡한 비동기 흐름에서는 `Task`를 사용하되, 반드시 **`CancellationToken`**을 전파합니다.

**[핵심 코드: Full Async Propagation]**
```csharp
// CancellationToken(ct)을 반드시 끝까지 전파하여 자원 낭비를 방지합니다.
public async Task<List<RouletteItem>> SpinMultiAsync(..., CancellationToken ct) {
    return await db.RouletteSpins
        .Include(s => s.Items)
        .ToListAsync(ct); // [v6.2] ct 전파 준수
}
```

---

## 🛡️ 3. 예외 및 오류 처리 (Error Handling)

시스템은 'Panic' 상태에 빠져서는 안 됩니다. 

### 🧬 Global Exception Middleware
수동적인 `try-catch` 남발보다는 전역 미들웨어를 통한 통합 예외 관리를 지향합니다.

**[핵심 코드: Global Exception Filter]**
```csharp
// Program.cs에서 가장 먼저 등록되어 함선의 모든 장애를 낚아챕니다.
app.UseMiddleware<ExceptionMiddleware>();

// [Middleware Logic]
try {
    await next(context);
} catch (Exception ex) {
    _logger.LogError(ex, "🔥 [심각한 오류 감지]");
    context.Response.StatusCode = 500;
    await context.Response.WriteAsJsonAsync(new { Error = "함선 내부에 알 수 없는 충돌이 발생했습니다." });
}
```

---

## 🎨 4. 네이밍 및 관례 (Conventions)

- **심플한 인터페이스**: `IAppDbContext`, `IRouletteService` 등 `I` 접두어 사용.
- **오시리스 수식어**: 내부 주석이나 로그 메시지에는 [오시리스의 시선], [이지스의 방패] 등 함선의 정체성을 드러내는 수식어를 권장합니다.

---

## 🧩 5. 아키텍처 및 모듈화 패턴 (Modular Architecture Patterns)

오시리스 함선은 거대한 모놀리스 구조를 탈피하여 **'모듈러 모놀리스(Modular Monolith)'** 및 **'순수 수직 분할(Pure Vertical Slice)'** 아키텍처를 지향합니다.

### 🔪 순수 수직 분할 (Pure Vertical Slice) 및 Single-File 패턴
- **Single-File 응집도**: 명령(Command/Query) 레코드, 검증 로직, 핸들러(Handler) 로직을 **단일 파일**에 함께 배치하여 캡슐화와 유지보수성을 극대화합니다. (예: `SpinRoulette.cs`)
- **얇은 위임자 (Thin Orchestrator)**: Controller나 레거시 Service 계층은 스스로 복잡한 비즈니스 로직을 처리하지 않습니다. 오직 MediatR의 `ISender.Send()`를 호출하여 모듈 내부의 핸들러에 제어권을 위임하는 중계 역할만 수행합니다.

### 🤝 모듈별 격리된 계약(Abstractions) 관리
- **모듈별 추상화 (Modular Abstractions)**: `MooldangBot.Contracts`의 비대화를 막기 위해, 각 기능 모듈(`Roulette`, `Point`, `Commands`, `SongBook`)은 자신의 인터페이스를 독자적인 `Abstractions` 폴더 내에 정의합니다. (예: `MooldangBot.Modules.Roulette/Abstractions/IRouletteDbContext.cs`)
- **수평적 참조 금지 (Layering strictly enforced)**: 모듈 간에 구체적인 핸들러나 내부 서비스를 직접 참조하는 것을 엄격히 금지합니다. 오직 `MediatR` 메시지나 공용 인터페이스(`Abstractions`)를 통해서만 상호작용합니다.
- **순환 참조 방지**: `Infrastructure`는 모든 모듈의 인터페이스를 구현하지만, 모듈은 서로를 모르고 오직 인프라가 제공하는 계약(Abstractions)에만 의존하게 함으로써 순환 참조의 고리를 완벽히 끊어냅니다.

### 🚀 고성능 JSON Source Generation
- **GC 부하 최소화**: 대규모 트래픽(10k TPS) 대응을 위해 리플렉션 대신 `System.Text.Json`의 Source Generator를 필수적으로 사용합니다.
- **Context 관리**: 모든 DTO는 `ChzzkJsonContext` 등에 등록하여 빌드 시점에 직렬화 코드를 생성하도록 관리합니다.

---

물멍! 🐶🚢✨
"선장님, 이 가이드라인이 지켜질 때 오시리스의 코드는 비로소 하나의 생명체처럼 유기적으로 공명하게 됩니다. 무거운 모놀리스의 낡은 갑옷을 벗고, 날렵한 수직 분할 모듈의 기동력을 마음껏 발휘하십시오!"
