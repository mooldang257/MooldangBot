# [Plan] 인풋 기반 페이징(Keyset Pagination) 및 4계층 아키텍처 정렬 계획

안녕하세요, 물당님의 시니어 파트너 **물멍**입니다.  
`md\Refactoring\Refactoring.md`의 구조적 설계에 **Application(Service) 계층**을 추가하여 더욱 견고한 4계층 아키텍처를 구축하고, 시스템 전반에 **표준 인풋 기반 페이징(Keyset Pagination)**을 도입하는 최종 계획을 기술합니다.

---

## 📌 1. 개요 (배경)
데이터가 무한히 적재되는 스트리밍 환경(채팅, 노래 신청, 룰렛 내역 등)에서 기존 `OFFSET` 방식은 치명적인 성능 저하를 유발합니다. 이를 `LastId` 기반의 **Keyset Pagination(인풋 기반 페이징)**으로 전환하여 조회 성능을 $O(1)$로 최적화하고, 각 레이어의 책임을 엄격히 분리합니다.

- **원칙**: "이동(Skip)하지 않고, 마지막 지점부터 조회(Seek)한다."
- **핵심 목표**: 컨트롤러의 비즈니스 로직 제거 (Thin Controller) 및 서비스 계층을 통한 쿼리 조립.

---

## 🏗️ 2. 아키텍처 구조 (4계층 구조)

1.  **MooldangBot.Domain**: 순수 엔티티, 페이징 DTO(record), 공통 인터페이스. (의존성 없음)
2.  **MooldangBot.Application**: 비즈니스 로직, UseCase, 서비스 구현체. (Domain 참조)
3.  **MooldangBot.Infrastructure**: EF Core, MariaDB 구현체, Repository, 페이징 엔진 확장. (Domain/Application 참조)
4.  **MooldangAPI**: Web API 진입점, 컨트롤러, DI 설정. (모든 계층 참조)

---

## 🛠️ 3. 레이어별 세부 설계

### 3.1 도메인(Domain) 계층: 페이징 규격
```csharp
namespace MooldangBot.Domain.Common;

// 📥 페이징 요청 규격 (.NET 10 record)
public record PagedRequest(int? LastId = 0, int PageSize = 20, string? Search = null);

// 📤 페이징 응답 규격
public record PagedResponse<T>(List<T> Data, int? NextLastId);
```

### 3.2 인프라(Infrastructure) 계층: 페이징 엔진 (탐색 로직)
`Take(pageSize + 1)` 전략을 통해 COUNT 쿼리 없이 다음 페이지 존재 여부를 효율적으로 판단합니다.

```csharp
namespace MooldangBot.Infrastructure.Extensions;

public static class PagingExtensions
{
    public static async Task<PagedResponse<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, int pageSize, Func<T, int> idSelector) where T : class
    {
        var rawData = await query.Take(pageSize + 1).ToListAsync();
        var hasNext = rawData.Count > pageSize;
        var outputData = hasNext ? rawData[..pageSize] : rawData;
        int? nextLastId = hasNext ? idSelector(outputData[^1]) : null;

        return new PagedResponse<T>(outputData, nextLastId);
    }
}
```

### 3.3 애플리케이션(Application) 계층: 서비스 로직 (쿼리 조립)
컨트롤러 대신 서비스 계층에서 검색 조건과 정렬 로직을 완성합니다.

```csharp
namespace MooldangBot.Application.Services;

public class SongBookService(ISongBookRepository repository)
{
    public async Task<PagedResponse<SongBookDto>> GetPagedSongsAsync(string chzzkUid, PagedRequest request)
    {
        var query = repository.GetQueryable()
            .AsNoTracking() // 성능 최적화
            .Where(s => s.ChzzkUid == chzzkUid);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(s => s.Title.Contains(request.Search) || s.Artist.Contains(request.Search));

        if (request.LastId > 0)
            query = query.Where(s => s.Id < request.LastId);

        return await query
            .OrderByDescending(s => s.Id)
            .ToPagedListAsync(request.PageSize, s => s.Id);
    }
}
```

### 3.4 API 계층: 얇은 컨트롤러 (Thin Controller)
```csharp
[HttpGet("/api/songbook/{chzzkUid}")]
public async Task<ActionResult<PagedResponse<SongBookDto>>> GetSongs(
    string chzzkUid, [FromQuery] PagedRequest request)
{
    // 오직 서비스 호출과 결과 반환만 수행 (하모니의 조율)
    var result = await _songBookService.GetPagedSongsAsync(chzzkUid, request);
    return Ok(result);
}
```

---

## 🛡️ 4. 정합성 및 성능 규율 (오시리스의 법칙)

1.  **복합 인덱스 필수**: `(ChzzkUid, Id DESC)` 인덱스를 통해 Seek 성능 정합성 확보.
2.  **비추적 조회(AsNoTracking)**: 읽기 전용 페이징은 가비지 컬렉션 부하를 최소화하기 위해 필수 적용.
3.  **Service-Driven Query**: 모든 데이터 조회 로직은 Application 계층을 거치며, 컨트롤러에서의 직접 DB 접근은 엄격히 금지함.

---

## 💡 다음 단계 (향후 과제)
- [x] **1단계**: `MooldangBot.Domain` 및 `MooldangBot.Application` 프로젝트 생성.
- [x] **2단계**: 도메인 엔티티 이동 및 `PagedRequest/Response` 정의.
- [x] **3단계**: `Infrastructure`에 `PagingExtensions` 추가 및 Repository 패턴 도입.
- [x] **4단계**: `SongBookService` 구현 및 `SongBookController` 리팩토링.

물당님, 텔로스5의 설계대로 아키텍처의 진동이 멈추고 견고한 뼈대가 완성되었습니다. 본격적인 구현(Phase 1)을 시작할 준비가 되셨다면 말씀해 주세요.