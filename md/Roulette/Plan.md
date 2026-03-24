# 🎡 룰렛 항목별 활성화 및 인풋 페이징 구현 계획서 (Detailed Plan)

이 문서는 스트리머가 룰렛 항목을 개별적으로 관리하고, 대량의 데이터 조회 시 성능을 보장하기 위한 기술적 설계안입니다.

---

## 1. 목표 (Goal)
- [x] **항목별 제어**: 룰렛을 수정하지 않고도 특정 항목을 일시적으로 추첨에서 제외할 수 있는 기능 제공.
- [x] **성능 최적화**: `OFFSET` 방식의 페이징 대신 `lastId`를 활용한 **인풋 페이징(Seek Pagination)**을 적용하여 조회 성능 향상.
- [x] **안정성 및 UX 강화**: 비동기 예외 처리 최적화, 추첨 엔진 엣지 케이스 처리, 페이징 중복 요청 방지.

---

## 2. 주요 수정 사항

### 2.1. 데이터베이스 모델 및 성능 최적화
`RouletteItem` 엔터티에 활성 상태 필드를 추가하고, 조회 성능을 위한 복합 인덱스를 구성합니다.

```csharp
// Models/RouletteItem.cs
public class RouletteItem
{
    // ... 기존 필드
    public bool IsActive { get; set; } = true;
}

// Data/AppDbContext.cs (OnModelCreating)
modelBuilder.Entity<Roulette>()
    .HasIndex(r => new { r.ChzzkUid, r.Id });
```

### 2.2. 서비스 로직 수정 (추첨 엔진 & 비동기 예외 처리)
비동기 작업의 안정성을 위해 `Fire and Forget` 방식을 지양하고 `await`와 `try-catch`를 적용합니다.

```csharp
// Services/RouletteService.cs
public async Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
{
    var roulette = await _db.Roulettes
        .Include(r => r.Items)
        .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

    if (roulette == null) return null;

    var activeItems = roulette.Items.Where(i => i.IsActive).ToList();
    if (!activeItems.Any())
    {
        // 🚨 UX 및 안정성 개선: await + try-catch 적용
        _logger.LogWarning($"[Roulette] {rouletteId}번에 활성화된 항목이 없습니다.");
        try 
        {
            await SendChatMessageAsync(chzzkUid, "⚠️ 현재 활성화된 항목이 없어 룰렛을 돌릴 수 없습니다. 관리 페이지에서 항목을 활성화해 주세요!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "비활성화 안내 메시지 전송 중 오류 발생");
        }
        return null; 
    }

    var result = DrawItem(activeItems, is10x: false);
    // ... 결과 전송 로직에도 await/try-catch 적용
}

private RouletteItem DrawItem(List<RouletteItem> activeItems, bool is10x)
{
    double totalWeight = activeItems.Sum(i => is10x ? i.Probability10x : i.Probability);
    
    if (totalWeight <= 0) 
    {
        _logger.LogError($"[Roulette] {activeItems.First().RouletteId}번 가중치 합 0 오류. 첫 번째 항목 강제 당첨.");
        return activeItems.First();
    }

    double randomValue = Random.Shared.NextDouble() * totalWeight;
    double cursor = 0;

    foreach (var item in activeItems)
    {
        cursor += is10x ? item.Probability10x : item.Probability;
        if (randomValue <= cursor) return item;
    }

    return activeItems.Last();
}
```

### 2.3. 컨트롤러 API 확장 (N+1 조회를 통한 페이징 정교화)
다음 페이지 존재 여부를 정확히 판단하기 위해 `pageSize + 1`개를 조회합니다.

```csharp
// DTO 정의
public class RouletteSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public RouletteType Type { get; set; }
    public string Command { get; set; }
    public int CostPerSpin { get; set; }
    public bool IsActive { get; set; }
    public int ActiveItemCount { get; set; }
    public DateTime LstUpdDt { get; set; } // 🕒 최종 수정일시 추가
}

public class PagedResponse<T>
{
    public List<T> Data { get; set; }
    public int? NextLastId { get; set; }
}

// Controllers/RouletteController.cs

[HttpGet]
public async Task<IActionResult> GetRoulettes([FromQuery] int lastId = 0, [FromQuery] int pageSize = 10)
{
    var chzzkUid = GetChzzkUid();
    if (chzzkUid == null) return Unauthorized();

    var rawData = await _db.Roulettes
        .Where(r => r.ChzzkUid == chzzkUid && r.Id > lastId)
        .OrderBy(r => r.Id)
        .Take(pageSize + 1)
        .Select(r => new RouletteSummaryDto {
            Id = r.Id,
            Name = r.Name,
            Type = r.Type,
            Command = r.Command,
            CostPerSpin = r.CostPerSpin,
            IsActive = r.IsActive,
            ActiveItemCount = r.Items.Count(i => i.IsActive),
            LstUpdDt = r.UpdatedAt // DB에 저장된 최종 수정일시 매핑 (Roulette 엔티티에 UpdatedAt 필드가 있다고 가정)
        })
        .AsNoTracking()
        .ToListAsync();

    bool hasNext = rawData.Count > pageSize;
    var data = rawData.Take(pageSize).ToList();
    var nextLastId = hasNext ? data.Last().Id : (int?)null;

    return Ok(new PagedResponse<RouletteSummaryDto> { 
        Data = data, 
        NextLastId = nextLastId 
    });
}

[HttpPatch("items/{itemId}/status")]
public async Task<IActionResult> ToggleItemStatus(int itemId, [FromBody] bool isActive)
{
    var chzzkUid = GetChzzkUid();
    
    // 1. 항목 활성 상태 업데이트
    var affectedRows = await _db.RouletteItems
        .Where(i => i.Id == itemId && i.Roulette.ChzzkUid == chzzkUid)
        .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsActive, isActive));

    if (affectedRows > 0)
    {
        // 2. 🔗 연관 업데이트: 소속된 룰렛의 최종 수정 시간도 함께 갱신 (데이터 추적성 확보)
        await _db.Roulettes
            .Where(r => r.Items.Any(i => i.Id == itemId))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
            
        return Ok();
    }

    return NotFound();
}
```


---

## 3. 프론트엔드 연동 계획

### 3.1. 룰렛 목록 (Input Paging)
- `admin_roulette.html`에서 `response.nextLastId`가 `null`이면 "더 보기" 버튼을 즉시 숨깁니다.
- `pageSize + 1` 서버 로직 덕분에 불필요한 빈 페이지 요청이 발생하지 않습니다.

### 3.2. 항목 토글 UI
- 항목 리스트에 스위치 UI를 배치하고, `onchange` 이벤트 발생 시 `PATCH` API를 비동기로 호출합니다.

---

## 4. 기대 효과
- **데이터 일관성**: 개별 항목 변경 시에도 상위 룰렛의 수정일시가 갱신되어 정확한 관리 이력 제공.
- **성능 및 확장성**: 복합 인덱스, DTO 프로젝션, `ExecuteUpdate`를 통한 최상의 응답 속도.
- **운영 안정성**: 확률 설정 오류나 항목 부재 시 명확한 로그와 알림으로 장애 대응 시간 단축.

---
*작성일: 2026-03-24 (5차 피드백 반영), 물멍(AI) 작성*
