# MooldangAPI 커서 기반 페이징(Cursor-based Pagination) 도입 상세 계획

## 1. 개요 및 목적
* **현재 상태:** `CommandsController.cs` 등에서 `Where(c => c.ChzzkUid == chzzkUid).ToListAsync()`를 통해 전체 데이터를 메모리에 올리고 있음.
* **문제점:** 데이터가 많아질 경우 MariaDB 부하 및 API 응답 지연 발생. 전통적인 Offset 페이징(`Skip`, `Take`)은 실시간 데이터 추가/삭제 시 누락이나 중복을 유발함.
* **해결책:** 인풋 페이징(Cursor-based Pagination)을 도입하여 `O(1)`의 인덱스 탐색 성능을 보장하고 실시간 스트리밍 환경에서의 데이터 안정성을 확보함.

## 2. 도메인 및 DTO 설계 (Models/DTOs.cs 수정)
커서 기반 페이징을 처리하기 위한 공통 요청/응답 DTO를 설계합니다. C# .NET 10의 `record`를 활용하여 불변 객체로 선언합니다.

### 📝 코드 스니펫: 공통 Paging DTO
```csharp
namespace MooldangAPI.Models
{
    // 커서 페이징 요청 DTO
    public record CursorPagedRequest(
        int? Cursor, // 마지막으로 조회한 항목의 고유 ID (처음 요청 시 null)
        int Limit = 20 // 한 번에 가져올 데이터 개수
    );

    // 커서 페이징 응답 DTO
    public record CursorPagedResponse<T>(
        List<T> Items,       // 조회된 데이터 목록
        int? NextCursor,     // 다음 페이지 조회를 위한 커서 (없으면 null)
        bool HasNext         // 다음 페이지 존재 여부
    );
}
```

## 3. Application Layer 적용 계획

### 3.1. CommandsController.cs 리팩토링
스트리머의 명령어 목록을 가져올 때 커서 페이징을 적용합니다. 기준 커서(Cursor)보다 큰 Id를 가진 데이터를 Limit만큼 조회합니다.

📝 **코드 스니펫: GetCommands 엔드포인트 변경안**
```csharp
[HttpGet("/api/commands/list/{chzzkUid}")]
public async Task<IResult> GetCommands(string chzzkUid, [AsParameters] CursorPagedRequest request)
{
    // 1. 커서 기반 쿼리 구성 (MariaDB 인덱스 활용)
    var query = _db.StreamerCommands
        .Where(c => c.ChzzkUid == chzzkUid)
        .OrderBy(c => c.Id) // Id 기준 정렬 필수
        .AsQueryable();

    if (request.Cursor.HasValue)
    {
        query = query.Where(c => c.Id > request.Cursor.Value);
    }

    // 2. Limit + 1 개를 조회하여 다음 페이지 존재 여부 파악
    var items = await query.Take(request.Limit + 1).ToListAsync();

    // 3. 응답 데이터 가공
    bool hasNext = items.Count > request.Limit;
    if (hasNext) items.RemoveAt(request.Limit); // 초과분 제거

    int? nextCursor = hasNext ? items.Last().Id : null;

    var response = new CursorPagedResponse<StreamerCommand>(items, nextCursor, hasNext);
    
    return Results.Ok(response);
}
```

### 3.2. SongController.cs 조회 기능 추가 및 페이징
현재 SongController.cs에는 신청곡 조회(List) 엔드포인트가 생략되어 있으나, 신청곡 큐(Queue)야말로 실시간으로 변동이 심하므로 커서 페이징이 필수적입니다.

📝 **코드 스니펫: GetSongQueue 엔드포인트 신설안**
```csharp
[HttpGet("/api/song/queue/{chzzkUid}")]
public async Task<IResult> GetSongQueue(string chzzkUid, [AsParameters] CursorPagedRequest request)
{
    // 신청곡은 먼저 신청된 순서(CreatedAt 또는 Id)로 재생되므로 Id 오름차순
    var query = _db.SongQueues
        .Where(s => s.ChzzkUid == chzzkUid && s.Status != "Completed")
        .OrderBy(s => s.Id)
        .AsQueryable();

    if (request.Cursor.HasValue)
    {
        query = query.Where(s => s.Id > request.Cursor.Value);
    }

    var songs = await query.Take(request.Limit + 1).ToListAsync();

    bool hasNext = songs.Count > request.Limit;
    if (hasNext) songs.RemoveAt(request.Limit);

    int? nextCursor = hasNext ? songs.Last().Id : null;

    return Results.Ok(new CursorPagedResponse<SongQueue>(songs, nextCursor, hasNext));
}
```

## 4. 인프라스트럭처 (MariaDB / EF Core 최적화)
* **인덱스 확인**: 커서로 사용되는 Id (Primary Key)와 ChzzkUid가 함께 사용되므로, 복합 인덱스 생성이 성능상 유리합니다. (`Index(nameof(ChzzkUid), nameof(Id))`)
* **쿼리 평가**: EF Core 로깅을 통해 LIMIT 구문이 MariaDB 서버 측에서 정확히 번역되어 실행되는지 확인(Client Evaluation 방지)해야 합니다.

## 5. 단계별 실행 계획 (Action Items)
1. `DTOs.cs`에 `CursorPagedRequest` 및 `CursorPagedResponse<T>` 추가
2. `AppDbContext` 내의 엔티티(`StreamerCommand`, `SongQueue`)에 복합 인덱스 설정 (필요 시 마이그레이션 생성)
3. `CommandsController.cs`의 `GetCommands` 메서드 수정
4. `SongController.cs`에 `GetSongQueue` 메서드 추가 (또는 기존 로직 수정)
5. 프론트엔드(`commands.html`, `dashboard.html`)에서 스크롤 시 `NextCursor`를 상태로 저장하여 무한 스크롤(Infinite Scroll) 또는 "더 보기" 버튼 로직으로 연동

## 6. 검증방법
1. Phase 1 -- 커서 페이징 쿼리 시뮬레이션
EXPLAIN SELECT * FROM unifiedcommands 
WHERE chzzkuid = 'TARGET_UID' AND id < 100 
ORDER BY id DESC 
LIMIT 21;

2. Phase 2 -- 오프셋 페이징 쿼리 시뮬레이션
EXPLAIN SELECT * FROM unifiedcommands 
WHERE chzzkuid = 'TARGET_UID' 
ORDER BY id DESC 
LIMIT 100 OFFSET 0;