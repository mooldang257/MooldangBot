# 03. Architecture & MediatR Rules

## 1. 개요
MooldangBot은 **4계층 클린 아키텍처(Clean Architecture)**를 지향합니다. 각 계층은 명확한 책임을 가지며, 특히 **MediatR**을 통해 계층 간 의존성을 느슨하게 유지합니다.

## 2. 4계층 구조와 의존성 흐름
의존성은 항상 고수준(Domain)에서 저수준(Infrastructure)으로 흐르지 않으며, 내부(Domain/Application)는 외부(Api/DB)를 알지 못해야 합니다.

1. **Domain**: 프로젝트의 심장. 외부 라이브러리 의존성 없이 순수한 엔티티와 비즈니스 규칙만 포함합니다.
2. **Application**: **[주력 계층]** MediatR 핸들러를 포함하며, 비즈니스 유스케이스(Logic)를 관장합니다.
3. **Infrastructure**: DB(EF Core), API 클라이언트, 메시징 등 외부 인프라 구현체입니다.
4. **Api / Presentation**: 외부 요청의 진입점. 최대한 얇게(Thin) 유지해야 합니다.

## 3. MediatR 핸들러 규칙 (Rich Handler vs Thin Controller)
컨트롤러는 단순히 요청을 `Application`으로 '던지는' 역할만 수행해야 합니다.

> [!TIP]
> **[Architect's Tip] MediatR 핸들러 자체가 'Service'입니다.**  
> Handler 내부에서 또 다른 Service 클래스를 주입받아 로직을 위임하는 오버 엔지니어링을 피하십시오. 예외적으로 재사용성이 극도로 높은 공통 로직(예: 외부 API 연동 등)에 한해서만 별도의 Service 클래스를 추출하여 주입합니다. 기본적으로는 **Handler가 IAppDbContext를 통해 직접 데이터를 조작하는 것**을 원칙으로 합니다.

❌ **Don't: 컨트롤러에 비즈니스 로직이 섞인 경우**
```csharp
[HttpPost]
public async Task<IActionResult> Join(ViewerJoinRequest req) 
{
    // [문제] 컨트롤러에서 직접 DB를 뒤적이고 로직을 처리합니다.
    var profile = await _db.ViewerProfiles.FirstOrDefaultAsync(v => v.Id == req.Id);
    if(profile == null) return BadRequest();
    profile.Points += 1000;
    await _db.SaveChangesAsync();
    return Ok();
}
```

✅ **Do: 전형적인 Thin Controller와 Rich Handler**
```csharp
// [API] 컨트롤러는 요청을 Command로 변환하여 보내기만 합니다.
[HttpPost]
public async Task<IActionResult> Join(ViewerJoinCommand command) 
    => Ok(await _mediator.Send(command));

// [Application] 실제 비즈니스 로직은 핸들러가 모두 책임집니다.
public record ViewerJoinCommand(int Id) : IRequest<bool>;

public class ViewerJoinCommandHandler(IAppDbContext db) : IRequestHandler<ViewerJoinCommand, bool> 
{
    public async Task<bool> Handle(ViewerJoinCommand req, CancellationToken ct) 
    {
        // 핸들러가 서비스이므로 db를 직접 다룹니다.
        var profile = await db.ViewerProfiles.FindAsync([req.Id], ct);
        if(profile == null) return false;
        profile.Points += 1000;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
```

## 4. 커맨드와 핸들러의 물리적 구성
가독성을 위해 **Command/Query 정의와 Handler는 하나의 `.cs` 파일에 함께 작성**하는 것을 권장합니다. 

- 파일명: `[FeatureName]Command.cs` 또는 `[FeatureName]Query.cs`
- 위치: `MooldangBot.Application/Features/[도메인명]/[Commands|Queries]`

## 5. 계층 간 데이터 전달 (DTO)
- **Request/Response**: `Presentation` 계층의 DTO는 `Application`으로 넘어올 때 반드시 `Command/Query` 객체로 변환되어야 합니다.
- **Entity**: `Domain` 엔티티는 가급적 `Application` 외부(Api 응답 등)로 직접 노출하지 않습니다.

---
**최종 승인**: 2026-04-02 (아키텍트 검토 완료 / 'Handler is Service' 원칙 적용)
