# 🔮 [Final Master Plan] 5계층 아키텍처 및 수직적 슬라이스 정렬 계획 (v2)

본 문서는 `md/Refactoring/plan.md`를 계승하여, 더욱 진화된 **5계층 아키텍처**와 **수직적 슬라이스(Vertical Slice)** 정렬, 그리고 **인풋 기반 페이징(Keyset Pagination)**의 전면 도입을 위한 최종 로드맵을 기술합니다.

---

## 🏗️ 1. 최종 아키텍처 조감도 (5-Layer Structure)

| 계층 | 프로젝트명 | 역할 및 핵심 가치 | 의존성 (References) |
| :--- | :--- | :--- | :--- |
| **Domain** | `MooldangBot.Domain` | 순수 엔티티, DTO, 공통 규격 (의존성 제로) | 없음 |
| **Application** | `MooldangBot.Application` | 비즈니스 로직, 기능별 서비스, 백그라운드 워커 | Domain |
| **Infrastructure** | `MooldangBot.Infrastructure` | DB(MariaDB) 통신, 페이징 엔진, 외부 API 연동 | Domain / Application |
| **Presentation** | `MooldangBot.Presentation` | 컨트롤러 및 SignalR 허브 (Web API 진입 관리) | Application |
| **API (Host)** | `MooldangAPI` | 순수 실행 진입점, DI 조립, 환경 설정 (.env) | 모든 계층 결합 (Bootstrapper) |

---

## 🚀 2. 수직적 슬라이스 (Feature Folder) 재배치

기존의 `Services/`, `Controllers/` 식의 수평적 구조에서 탈피하여, **기능(Feature)** 단위로 로직을 정렬합니다.

- **범위**: `Application`, `Presentation` 프로젝트 내부
- **핵심 슬라이스**:
    1. **SongBook**: 노래 리스트/오마카세 신청 큐 관리
    2. **Roulette**: 룰렛 상태 물리 및 결과 히스토리
    3. **Overlay**: 오버레이 렌더링 및 UI 상태 동기화
    4. **ChatPoints**: 시청자 채팅 포인트 적립 및 상점
    5. **ChatAndDonation**: 채팅 파싱 및 후원(치즈) 연동 처리
    6. **AvatarSettings**: 팬 캐릭터 및 아바타 커스텀 상태

---

## 📥 3. 인풋 기반 페이징 (Keyset Pagination) 도입

데이터가 무한히 쌓이는 스트리밍 환경에서 `OFFSET` 방식의 성능 저하를 방지하기 위해 `LastId` 기반 탐색을 강제합니다.

### 3.1 공통 응답 규격 (Domain)
```csharp
namespace MooldangBot.Domain.Common;

// 📥 인풋 페이징 요청
public record PagedRequest(int? LastId = 0, int PageSize = 20, string? Search = null);

// 📤 인풋 페이징 응답
public record PagedResponse<T>(List<T> Data, int? NextLastId);
```

### 3.2 페이징 엔진 (Infrastructure)
```csharp
namespace MooldangBot.Infrastructure.Extensions;

public static class PagingExtensions
{
    public static async Task<PagedResponse<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, int pageSize, Func<T, int> idSelector) where T : class
    {
        // 1개 더 가져와서 다음 페이지 여부 확인
        var rawData = await query.Take(pageSize + 1).ToListAsync();
        var hasNext = rawData.Count > pageSize;
        var outputData = hasNext ? rawData[..pageSize] : rawData;
        int? nextLastId = hasNext ? idSelector(outputData[^1]) : null;

        return new PagedResponse<T>(outputData, nextLastId);
    }
}
```

### 3.3 서비스 레이어 구현 (Application/Features/SongBook)
```csharp
public class SongBookService(IAppDbContext db)
{
    public async Task<PagedResponse<SongHistoryDto>> GetHistoryAsync(string chzzkUid, PagedRequest req)
    {
        var query = db.SongHistories
            .AsNoTracking()
            .Where(h => h.ChzzkUid == chzzkUid);

        // 🔍 인풋 기반 필터링 (Where Id < LastId)
        if (req.LastId > 0)
            query = query.Where(h => h.Id < req.LastId);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(h => h.SongTitle.Contains(req.Search));

        return await query
            .OrderByDescending(h => h.Id) // 인덱스 최적화 필수 (ChzzkUid, Id DESC)
            .ToPagedListAsync(req.PageSize, h => h.Id);
    }
}
```

### 3.4 엔티티 인덱스 규율 (Domain/Entities - 오시리스의 법칙)
데이터 조회 성능을 결정짓는 인덱스는 RAW SQL이 아닌 **코드로 관리**되어야 합니다.

```csharp
using Microsoft.EntityFrameworkCore;

// 🛡️ ChzzkUid(ASC), Id(DESC) 복합 인덱스로 Seek 성능 정합성 확보
[Index(nameof(ChzzkUid), nameof(Id), IsDescending = new[] { false, true })]
public class SongHistory
{
    public int Id { get; set; }
    public string ChzzkUid { get; set; } = null!;
    // ...
}
```

---

## 📅 4. 실행 로드맵 (Action Plan)

### Phase 1: 기반 인프라 구축
- [x] `MooldangBot.Presentation` 클래스 라이브러리 생성 (사용자 완료)
- [x] `Infrastructure`에 `PagingExtensions` 및 `PagedRequest/Response` 규격화
- [x] `Program.cs` 다이어트 준비 (DI 확장 메서드 스캐폴딩)

### Phase 2: 수직적 슬라이스 정렬 (Migration)
- [ ] **Roulette/SongBook**: 기존 `Features/` 폴더의 핸들러 및 서비스를 `Application` 프로젝트의 해당 기능 폴더로 이동
- [ ] **Controllers/Hubs**: 모든 컨트롤러를 `Presentation`으로 이동 및 네임스페이스 리팩토링

### Phase 3: 워커(Worker) 통합
- [ ] `ChzzkBackgroundService`와 `Worker`를 `Application/Workers`로 이동
- [ ] `MooldangAPI`는 순수하게 `Program.cs`만 남김

### Phase 4: 성능 최적화 및 인덱스 강제 (Code-First)
- [ ] **인덱스 규율 적용**: 모든 페이징 대상 엔티티에 `[Index]` 어트리뷰트 적용 및 EF Core Migration 생성
- [ ] 모든 대량 조회 API에 `PagedRequest` 적용 여부 전수 조사

---

물당님, 사용자의 조언을 받들어 **"오시리스의 규율은 코드로 남아야 합니다"**라는 원칙을 Phase 4에 완벽히 녹여냈습니다. 이제 DB 인덱스조차 시스템의 일부로서 엄격하게 관리될 것입니다. 구현을 시작할 준비가 되셨다면 말씀해 주세요.
