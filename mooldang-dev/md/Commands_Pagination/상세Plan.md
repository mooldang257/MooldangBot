# 📑 통합 명령어 인풋 페이징 상세 계획 (v1.5)

## 1. 개요
현재 적용된 커서 기반 페이징(Cursor-based)은 성능 면에서 우수하나, 특정 페이지로의 즉각적인 이동(Jump)이 어렵다는 단점이 있습니다. 스트리머의 관리 편의성을 위해 **직접 페이지 번호를 입력하여 이동하는 '인풋 페이징(Input Paging)'** 체계로 다시 전환하되, 인덱스를 활용하여 최적화된 오프셋 쿼리를 설계합니다.

## 2. 기술적 사양 (Technical Specifications)

### 2-1. 백엔드 (C# .NET 10 / MariaDB)
- **방식**: 최적화된 오프셋 페이징 (`Skip` & `Take`)
- **최적화**: `chzzkuid` 및 `id` 복합 인덱스를 태워 커버링 인덱스(Covering Index) 효과 유도
- **통합**: `UnifiedPagedResponse<T>` DTO를 통한 전체 데이터 건수 및 페이지 정보 제공

### 2-2. 프론트엔드 (Vanilla JS)
- **UI**: 번호 입력 필드와 엔터키 트리거를 활용한 직접 이동 기능
- **연동**: `GET /api/commands/unified/{chzzkUid}?page=X&pageSize=Y` 호출

## 3. 구현 세부 계획 (Milestones)

| 단계 | 항목 | 상세 내용 | 상태 |
| :--- | :--- | :--- | :--- |
| **1단계** | DTO 복원 및 확장 | 중복 제거 및 `UnifiedPagedResponse` 단일화 | [x] |
| **2단계** | 컨트롤러 로직 수정 | 오프셋 기반 인풋 페이징 API 전환 및 최적화 | [x] |
| **3단계** | 프론트엔드 UI 정비 | `commands.html` UI 연동 및 점프 기능 활성화 | [x] |
| **4단계** | 성능/타입 검증 | `dotnet build` 완료 및 JS 예외 처리 강화 | [x] |

## 4. 코드 스니펫 (Code Snippets)

### Backend: Optimized Offset Paging
```csharp
public async Task<IResult> GetUnifiedCommands(string chzzkUid, int page = 1, int pageSize = 10)
{
    var targetUid = chzzkUid.Trim().ToLower();
    var query = _db.UnifiedCommands
        .IgnoreQueryFilters()
        .Where(c => c.ChzzkUid == targetUid)
        .OrderByDescending(c => c.Id);

    int totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new UnifiedPagedResponse<T>(items, totalCount, page, pageSize));
}
```

### Frontend: Input Pagination Manager
```javascript
function renderInputPagination(data) {
    const container = document.getElementById('pagination-container');
    container.innerHTML = `
        <input type="number" id="pageJump" value="${data.currentPage}" 
               onkeypress="if(event.key === 'Enter') loadCommands(this.value)">
        <span>/ ${data.totalPages}</span>
    `;
}
```

---
**"인풋 페이징은 관리자에게 전역적인 시야와 즉각적인 통제권을 부여합니다."**
작성일: 2026-03-28
작성자: 시니어 파트너 물멍
