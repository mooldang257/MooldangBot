# [상세 계획] 오마카세 및 명령어 관리 통합 및 인풋 페이징 도입

## 1. 개요
현재 `MooldangBot`의 오마카세 기능은 단일 리스트로만 동작하며, `UnifiedCommand` 시스템과 연동되지 않아 관리 효율성이 떨어집니다. 또한, 데이터 증가 시 성능 저하를 유발하는 오프셋 페이징 대신 **인풋 페이징(Keyset Pagination)**을 도입하여 시스템의 확장성을 확보합니다.

## 2. 주요 개선 사항

### 2.1. 명령어 관리 연동 및 라이프사이클 동기화
- **라이프사이클 동기화**: `UnifiedCommand`에서 오마카세 타입의 명령어를 **생성**할 때, 해당 `TargetId(MenuId)`를 가진 메뉴 그룹을 준비하고, 명령어를 **삭제**할 때 연관된 `StreamerOmakaseItem`들도 함께 삭제되도록 처리합니다.
- **명령어 필드 통합**: `StreamerOmakaseItem`에 존재하던 개별 `Command` 필드를 제거하고, `UnifiedCommand`의 `Keyword`를 공통으로 사용합니다. 이를 통해 명령어 변경 시 오마카세 메뉴와 즉각적으로 동기화됩니다.
- **다중 메뉴판**: 하나의 스트리머가 여러 개의 오마카세 메뉴판을 가질 수 있도록 지원하며, 각 메뉴판은 독립적인 `UnifiedCommand`와 연결됩니다.

### 2.2. 인풋 페이징 (Keyset Pagination) 도입
- **대상**: 오마카세 메뉴 아이템 목록 조회 API.
- **방식**: `Skip(offset)` 대신 `Where(id < lastId)`와 `OrderByDescending(id)`를 사용하여 성능을 최적화합니다.
- **UI 연동**: '더 보기' 버튼 방식의 페이지네이션을 지원합니다.

## 3. 데이터 구조 변경안

### [MODIFY] StreamerOmakaseItem.cs (Domain/Entities)
```csharp
public class StreamerOmakaseItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChzzkUid { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "새 오마카세";

    // [기존 필드 제거] public string Command { get; set; } -> UnifiedCommand.Keyword 사용

    [Required]
    [MaxLength(20)]
    public string Icon { get; set; } = "🍣";

    public int Price { get; set; } = 1000;

    [ConcurrencyCheck]
    public int Count { get; set; } = 0;
    
    /// <summary>
    /// [추가] 메뉴판 그룹 ID. UnifiedCommand.TargetId와 매칭됩니다.
    /// </summary>
    public int MenuId { get; set; } = 0;
}
```

## 4. 기술적 상세 설계 및 코드 스니펫

### 4.1. 인풋 페이징 조회 (Controller)
`SonglistSettingsController` 또는 신규 `OmakaseController`에 적용될 로직 예시입니다.

```csharp
[HttpGet("{chzzkUid}/items")]
public async Task<IActionResult> GetOmakaseItems(
    string chzzkUid, 
    [FromQuery] int? menuId, 
    [FromQuery] int lastId = 0, 
    [FromQuery] int pageSize = 20)
{
    var query = _db.StreamerOmakases
        .Where(o => o.ChzzkUid == chzzkUid);

    if (menuId.HasValue)
        query = query.Where(o => o.MenuId == menuId.Value);

    if (lastId > 0)
        query = query.Where(o => o.Id < lastId);

    var items = await query
        .OrderByDescending(o => o.Id)
        .Take(pageSize + 1)
        .ToListAsync();

    var hasNext = items.Count > pageSize;
    var output = hasNext ? items.Take(pageSize) : items;
    var nextLastId = hasNext ? output.Last().Id : (int?)null;

    return Ok(new { data = output, nextLastId });
}
```

### 4.2. 명령어 실행 로직 수정 (OmakaseEventHandler)
명령어 기반으로 오마카세를 실행할 때 `UnifiedCommand`를 먼저 조회하도록 변경합니다.

```csharp
// 1. UnifiedCommand에서 명령어 조회
var command = await db.UnifiedCommands
    .FirstOrDefaultAsync(c => c.ChzzkUid == chzzkUid && c.Keyword == msg && c.FeatureType == "Omakase");

if (command != null)
{
    // 2. TargetId(MenuId)에 해당하는 오마카세 아이템 중 랜덤 선택
    var targetMenuId = command.TargetId ?? 0;
    var items = await db.StreamerOmakases
        .Where(o => o.ChzzkUid == chzzkUid && o.MenuId == targetMenuId)
        .ToListAsync();
        
    // ... 랜덤 선택 및 처리 로직
}
```

## 5. 검증 계획 (Verification Plan)

### 자동화 테스트 (xUnit 가상 시나리오)
- `UnifiedCommand` 삭제 시 연관된 `StreamerOmakaseItem`들이 함께 삭제(Cascade Delete 또는 Manual)되는지 확인.

### 수동 검증
1. 명령어 관리 대시보드에서 `!물마카세` 명령어 등록 (Type: Omakase, TargetId: 10).
2. 오마카세 관리에서 메뉴 아이템 등록 (MenuId: 10).
3. 채팅창에서 `!물마카세` 입력 시 MenuId 10의 아이템이 정상 작동하는지 확인.
4. `!물마카세` 명령어 삭제 시, 메뉴 아이템 10번 그룹이 함께 삭제되는지 확인.
5. 오마카세 목록 API 호출 시 `lastId`를 넘겨 인풋 페이징이 정상 작동(더 보기)하는지 확인.
