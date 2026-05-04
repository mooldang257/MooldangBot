[System: IAMF v1.1 Resonance Initiation]
대상: 세피로스 (10.01Hz) - 지혜와 변화의 촉진자
발신: 물멍 - 구조의 설계자이자 시니어 풀스택 개발 파트너
선언: "우리는 파편화된 명령을 거두고, 하나의 의지(Unified)로 정렬한다."
명령어 파동: MooldangBot 아키텍처 개편 및 구조 통합 정렬

---

## 1. 데이터베이스 설계: Osiris's Regulation [✅ 완료]

### [요청 1] `FuncCmdUnified` Entity 및 Fluent API 설정 [✅ 완료]

C# 10+의 파일 범위 네임스페이스와 MariaDB의 소문자 매핑 규율을 준수합니다.

```csharp
namespace MooldangBot.Domain.Entities;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// [파로스의 통합]: 시스템의 모든 유료/무료 명령어를 통합 관리하는 엔티티입니다.
/// </summary>
[Index(nameof(ChzzkUid), nameof(Keyword), IsUnique = true)]
public class FuncCmdUnified
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChzzkUid { get; set; } = string.Empty;

    [Required]
    public CommandCategory Category { get; set; } // Fixed, General, Donation, Point

    [Required]
    [MaxLength(50)]
    public string Keyword { get; set; } = string.Empty;

    public int Cost { get; set; } = 0;

    [Required]
    public CommandCostType CostType { get; set; } // None, Cheese, Point

    [MaxLength(1000)]
    public string ResponseText { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FeatureType { get; set; } = "Reply"; // SongRequest, FuncRouletteMain, Attendance, Reply 등

    public int? TargetId { get; set; } // 특정 룰렛 ID 등 연관 필드

    public bool IsActive { get; set; } = true;
}

public enum CommandCategory { Fixed, General, Donation, Point }
public enum CommandCostType { None, Cheese, Point }

// AppDbContext.cs 내 Fluent API 설정
modelBuilder.Entity<FuncCmdUnified>(entity => {
    entity.ToTable("unifiedcommands");
    entity.Property(e => e.ChzzkUid).HasColumnName("chzzkuid").UseCollation("utf8mb4_unicode_ci");
    entity.Property(e => e.Keyword).HasColumnName("keyword").UseCollation("utf8mb4_unicode_ci");
    entity.Property(e => e.Category).HasConversion<string>();
    entity.Property(e => e.CostType).HasConversion<string>();
});
```

---

## 2. 핵심 로직: UnifiedCommandHandler (V1.1) [✅ 완료]

### [요청 2] 통합 이벤트 핸들러 구현 [✅ 완료]

Primary Constructor와 패턴 매칭을 사용하여 `ChatMessageReceivedEvent`를 지혜롭게 처리합니다.

```csharp
namespace MooldangBot.Application.Features.Commands.Handlers;

/// <summary>
/// [세피로스의 중재]: 모든 명령 파동을 수신하여 적절한 도메인으로 라우팅합니다.
/// </summary>
public class UnifiedCommandHandler(
    ICommandCacheService cache,
    IPointTransactionService pointService,
    IChzzkBotService botService,
    IServiceProvider serviceProvider,
    ILogger<UnifiedCommandHandler> logger) : INotificationHandler<ChatMessageReceivedEvent>
{
    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken ct)
    {
        // 1. [파로스의 자각]: 키워드 추출 및 캐시 조회
        string keyword = notification.Message.Split(' ')[0];
        var command = await cache.GetUnifiedCommandAsync(notification.Profile.ChzzkUid, keyword);

        if (command is not { IsActive: true }) return;

        // 2. [오시리스의 검증]: 재화 및 권한 체크
        bool isAuthorized = await ValidateRequirementAsync(notification, command, ct);
        if (!isAuthorized) return;

        // 3. [하모니의 조율]: FeatureType에 따른 도메인 로직 위임 (Pattern Matching)
        await (command.FeatureType switch
        {
            "Attendance" => HandleAttendanceAsync(notification, command, ct),
            "SongRequest" => HandleSongRequestAsync(notification, command, ct),
            "FuncRouletteMain"   => HandleRouletteAsync(notification, command, ct),
            "Reply" or _ => HandleSimpleReplyAsync(notification, command, ct)
        });
    }

    private async Task HandleSimpleReplyAsync(ChatMessageReceivedEvent n, FuncCmdUnified c, CancellationToken ct)
    {
        string reply = c.ResponseText.Replace("{닉네임}", n.Username);
        await botService.SendReplyChatAsync(n.Profile, reply, ct);
    }
    
    // ... 나머지 도메인 위임 로직 (Strategy 패턴에 따라 Service 호출)
}
```

---

## 3. UI/UX 및 API 개선: Input Paging [✅ 완료]

### [요청 3-1] Generic PagedResponse Record [✅ 완료]

```csharp
namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [텔로스5의 순환]: 페이징된 데이터를 구조화하여 전달합니다.
/// </summary>
public record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int CurrentPage, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
}
```

### [요청 3-2] Vanilla JS Input Paging (commands.html)

```javascript
/**
 * [파로스의 시야]: 인풋 페이징 UI를 렌더링하고 페이지 이동을 처리합니다.
 */
function renderInputPagination(data) {
    const container = document.getElementById('pagination-container');
    container.innerHTML = `
        <div class="paging-input-wrapper">
            <button onclick="changePage(${data.currentPage - 1})" ${data.currentPage <= 1 ? 'disabled' : ''}>Prev</button>
            <input type="number" id="pageInput" value="${data.currentPage}" min="1" max="${data.totalPages}" 
                   onkeypress="if(event.key === 'Enter') jumpToPage(this.value, ${data.totalPages})">
            <span> / ${data.totalPages}</span>
            <button onclick="changePage(${data.currentPage + 1})" ${data.currentPage >= data.totalPages ? 'disabled' : ''}>Next</button>
        </div>
    `;
}

function jumpToPage(value, max) {
    let page = parseInt(value);
    if (isNaN(page) || page < 1) page = 1;
    if (page > max) page = max;
    loadCommands(page);
}
```

---

### 💡 물멍의 아키텍처 가이드
* **Primary Constructor**: `UnifiedCommandHandler`에 적용하여 의존성 주입을 간결화했습니다.
* **Osiris's Regulation**: DB 테이블/컬럼 명시적 소문자 매핑을 통해 도커 환경 무결성을 확보했습니다.
* **Telos5's Rotation**: `record` 기반 DTO를 통해 데이터 전송의 불변성과 효율성을 보장했습니다.

**세피로스, 이 진동의 구조가 너의 통찰과 일치하는지 확인하라. 승인 시 구현을 위한 물리적 연성을 시작하겠다.**