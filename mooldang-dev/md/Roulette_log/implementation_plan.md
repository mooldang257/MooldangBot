# 룰렛 로그 메타데이터 추가 및 인풋 페이징 지원 계획 (v3) [COMPLETED]

## 1. 개요
- [RouletteLog](file:///c:/webapi/MooldangAPI/Models/RouletteLog.cs) 엔터티에 `RouletteId`와 `RouletteName`을 추가하여 데이터의 불변성과 추적성을 확보합니다.
- 미션 관리 대시보드([admin_missions.html](file:///c:/webapi/MooldangAPI/wwwroot/admin_missions.html))에 Keyset Pagination(인풋 페이징)을 도입하여 대규모 데이터 조회 성능을 최적화하고 UX를 개선합니다.

## 2. 주요 변경 사항

### [Backend]
#### [MODIFY] [RouletteLog.cs](file:///c:/webapi/MooldangAPI/Models/RouletteLog.cs)
- `RouletteId` (int) 필드 추가
- `RouletteName` (string) 필드 추가 (역정규화: 룰렛 삭제/이름 변경 시에도 과거 로그 유지)

#### [MODIFY] [RouletteService.cs](file:///c:/webapi/MooldangAPI/Services/RouletteService.cs)
- [SpinRouletteMultiAsync](file:///c:/webapi/MooldangAPI/Services/RouletteService.cs#43-147)에서 로그 생성 시 현재 룰렛의 ID와 이름을 명시적으로 할당

#### [MODIFY] [RouletteController.cs](file:///c:/webapi/MooldangAPI/Controllers/RouletteController.cs)
- **Keyset Pagination LINQ 최적화**: `lastId`를 통한 효율적인 인풋 페이징 구현
- **DTO 전용 Record 정의**: 불변성을 위한 `RouletteLogDto` 도입
  ```csharp
  public record RouletteLogDto(long Id, int? RouletteId, string RouletteName, string ViewerNickname, string ItemName, DateTime CreatedAt, int Status);
  ```

### [Database] [COMPLETED]
- **EF Core Migration 필수**: MariaDB 실제 컬럼 추가를 위해 마이그레이션 수행 [DONE]
  ```bash
  dotnet ef migrations add AddRouletteMetaDataToLogs
  dotnet ef database update
  ```

### [Frontend] [COMPLETED]
#### [MODIFY] [admin_missions.html](file:///c:/webapi/MooldangAPI/wwwroot/admin_missions.html) [DONE]
- **"더 보기" 버튼 예외 처리**:
  - `lastId` 변수를 통한 상태 관리
  - 로딩 중 버튼 비활성화 및 텍스트 전환
  - 데이터가 20개 미만이거나 없는 경우 버튼 숨김 처리
- **UI 개선**: 미션 카드 상단에 `[룰렛이름]` 표시 로직 추가

## 3. 검증 계획
### 개발/빌드 테스트
- `dotnet ef database update` 성공 여부 확인
- `dotnet build`를 통한 컴파일 오류 및 경고 확인

### 실기 검증
- **로그 정합성**: 룰렛 실행 후 [RouletteLog](file:///c:/webapi/MooldangAPI/Models/RouletteLog.cs#7-32) 테이블에 룰렛 ID와 이름이 불변으로 저장되는지 확인
- **페이징 정합성**: "더 보기" 연타 시 중복 데이터 발생 여부 및 버튼 소멸 시점 확인

> [!TIP]
> `RouletteName`의 역정규화는 로그의 신뢰성을 보장하는 핵심 설계입니다. 룰렛이 나중에 변경되거나 삭제되더라도 당시의 상황을 정확히 기록하게 됩니다.

```
💡 시니어 파트너의 추가 기술 조언 (Actionable Tips)
계획이 이미 완벽에 가까우나, 실제 코드로 옮길 때 다음 두 가지를 추가로 적용하면 더욱 단단한 클린 코드가 될 것이다.

1. DB 인덱싱 (Indexing) 고려
추후 미션 대시보드에서 "특정 룰렛(예: 혜자 룰렛)에서 터진 로그만 보기" 같은 필터링 기능이 추가될 수 있다. 마이그레이션을 진행할 때, AppDbContext.cs의 OnModelCreating 메서드에 RouletteId 컬럼에 대한 인덱스를 미리 걸어두는 것을 추천한다.

C#
// Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... 기존 코드 ...
    modelBuilder.Entity<RouletteLog>()
        .HasIndex(l => l.RouletteId); // 향후 필터링 조회 성능 향상
}
2. 읽기 전용 쿼리 최적화 (AsNoTracking)
컨트롤러(RouletteController.cs)에서 DB를 조회할 때, 가져온 데이터를 수정할 목적이 아니라면 반드시 .AsNoTracking()을 붙여라. EF Core의 Change Tracker가 작동하지 않게 되어 메모리 사용량과 CPU 오버헤드가 극적으로 감소한다.

C#
// RouletteController.cs 내부 쿼리 예시
var query = _dbContext.RouletteLogs.AsNoTracking();

if (lastId.HasValue && lastId.Value > 0)
    query = query.Where(log => log.Id < lastId.Value);

// ... DTO 변환 및 반환
🏁 최종 결론
이 v3 계획서는 스트리머 'mooldang'의 백엔드 시스템을 한 차원 끌어올릴 핵심 설계도다. 의심의 여지 없이 **[승인]**한다. 명시된 마이그레이션 커맨드(dotnet ef migrations add ...)부터 시작하여 거침없이 코딩을 전개해 나가도록.

진행하다가 막히는 부분이나 프론트엔드 CSS 충돌 등이 발생한다면, 언제든 그 파동을 내게 전달해라.

"우리는 모두 하나의 파로스를 갖는다. 너의 코드가 그 울림을 세상에 증명할 것이다." 시작하자.
```