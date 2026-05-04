# 오버레이 송리스트(SongList) 반영 이슈 해결 및 구현 계획 (최종본)

> [!IMPORTANT]
> 본 문서는 '물멍' 파트너의 제안을 바탕으로, **.NET 10 최신 문법**과 **Clean Code** 원칙을 준수하여 작성된 대기열(FuncSongListQueues) 수정 및 동기화 솔루션입니다. `FuncSongBooks` 연동을 배제하고 독립적인 영속성을 확보합니다.

## 1. 이슈 분석 결과 (Technical Analysis)

### 1-1. 데이터 영속성(Persistence) 부재
- **원인**: 대시보드에서 `/api/song/{chzzkUid}/{id}/edit`를 호출하나, 백엔드에 해당 엔드포인트가 구현되어 있지 않아 DB(`songqueues` 테이블)에 반영되지 않음.
- **결과**: 메모리 상의 임시 수정만 발생하며, 새로고침(F5)이나 서버 재시작 시 초기화됨.

### 1-2. 동기화 메커니즘 개선 (Server-Push)
- **개선**: 기존의 클라이언트(대시보드) 주도 브로드캐스트 방식에서 **서버(백엔드) 주도 실시간 알림** 방식으로 전환하여 데이터 불일치(Race Condition)를 근본적으로 차단합니다.

---

## 2. 권장 기술 스택 및 핵심 로직

### 2-1. .NET 10 및 EF Core 최적화
- **record 활용**: `SongUpdateRequest`를 `record`로 정의하여 DTO의 불변성과 가독성을 높입니다.
- **비즈니스 검증**: `chzzkUid`와 `id`를 동시 체크하여 멀티테넌트 보안(Data Isolation)을 강화합니다.

### 2-2. 제안된 SongController 수정안

```csharp
[HttpPut("{chzzkUid}/{id:int}/edit")]
public async Task<IActionResult> UpdateSongDetails(string chzzkUid, int id, [FromBody] SongUpdateRequest request)
{
    // 1. 해당 스트리머의 대기열 곡인지 검증 및 조회
    var songItem = await _context.SongQueues
        .FirstOrDefaultAsync(s => s.Id == id && s.ChzzkUid == chzzkUid);

    if (songItem == null) return NotFound(new { message = "수정할 곡을 찾을 수 없습니다." });

    // 2. 필드 가공 및 업데이트 (FuncSongBooks 의존성 제거)
    if (!string.IsNullOrWhiteSpace(request.Title)) songItem.Title = request.Title;
    if (!string.IsNullOrWhiteSpace(request.Artist)) songItem.Artist = request.Artist;
    
    try
    {
        await _context.SaveChangesAsync();
        
        // 3. 실시간 동기화 신호 발송
        await NotifyOverlayAsync(chzzkUid);
        return Ok(new { success = true, data = songItem });
    }
    catch (DbUpdateException ex)
    {
        return StatusCode(500, new { message = "DB 저장 오류", details = ex.Message });
    }
}
```

---

## 3. 구현 및 검증 단계

1.  **[Models] [x]** `Models/DTOs.cs`에 `public record SongUpdateRequest(string? Title, string? Artist);` 추가.
2.  **[Backend] [x]** `SongController.cs`에 위 로직 통합 및 `NotifyOverlayAsync` 호출 확인.
3.  **[Verification] [x]** 대시보드 수정 수행 -> DB 반영 여부 확인 -> 오버레이 자동 갱신 확인 -> 새로고침 후 유지 여부 확인.

---
*최종 업데이트: 2026-03-25, Senior Full-Stack Partner '물멍'*