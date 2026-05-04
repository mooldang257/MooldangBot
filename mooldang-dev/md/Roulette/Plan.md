# 🎵 노래책 및 송리스트 오버레이 개선 기술 설계서

본 문서는 노래책(Songbook) 시스템의 인풋 페이징 구현과 오버레이 버그(`undefined` 표시) 수정을 위한 상세 기술 설계안입니다.

---

## 1. 오버레이 버그 분석 및 해결 (Issue Fix)

### 1.1. 원인 분석
- **현상**: `songlist_overlay.html`에서 곡 제목과 가수가 `undefined`로 출력됨.
- **원인**: `Program.cs`에서 JSON 직렬화 정책이 `PropertyNamingPolicy = null`(PascalCase 유지)로 설정되어 있으나, 프론트엔드 JS에서는 `song.title`, `song.artist`와 같이 lowercase로 접근함.
- **해결 방안**: 프론트엔드 코드를 `song.Title`, `song.Artist`로 수정하여 데이터 정합성을 확보함.

---

## 2. 노래책(Songbook) 인풋 페이징 설계

### 2.1. 인풋 페이징 (Seek Pagination) 도입
`OFFSET` 방식의 페이징(Page 1, 2...)은 데이터가 많아질수록 성능이 비선형적으로 저하됩니다. 이를 해결하기 위해 `LastId`를 활용한 정렬 및 조회를 수행합니다.

#### API 명세 (Draft)
- **Method**: `GET /api/songbook/{chzzkUid}`
- **Query Params**:
  - `LastId`: 마지막으로 조회된 곡의 ID (기본값 0)
  - `PageSize`: 한 페이지당 로드할 개수 (기본값 20)
  - `Search`: 제목 또는 가수 검색어 (선택 사항)

#### SQL 및 LINQ 최적화
```csharp
var query = _db.SongBooks
    .Where(s => s.ChzzkUid == chzzkUid && (lastId == 0 || s.Id < lastId))
    .OrderByDescending(s => s.Id)
    .Take(pageSize + 1);
```

### 2.2. 데이터베이스 스키마
`FuncSongBooks` 테이블은 스트리머의 전체 레퍼토리(노래책)를 관리합니다.

| 필드명 | 데이터 타입 | 제약 조건 | 설명 |
|---|---|---|---|
| **Id** | int | PK, AI | 고유 식별자 |
| **ChzzkUid** | string(50) | Indexed | 스트리머 UID |
| **Title** | string(200) | Required | 곡 제목 |
| **Artist** | string(100) | - | 가수 이름 |
| **IsActive** | bool | Default(true) | 신청 가능 여부 |
| **UsageCount** | int | Default(0) | 지금까지 신청된 횟수 |

---

## 3. 프론트엔드 구현 전략

### 3.1. 관리자 UI (`admin_songbook.html`)
- `admin_roulette.html`의 인풋 페이징 UI 패턴을 재사용합니다.
- 사용자가 하단으로 스크롤하거나 "더 보기" 버튼을 누르면 `LastId`를 전달하여 다음 데이터를 fetch합니다.

### 3.2. 오버레이 데이터 연동
- 오버레이 렌더링 시 전역 JSON 명명 정책(PascalCase)을 철저히 준수하도록 수정합니다.

---

## 4. 기대 효과
- **성능 보장**: 수천 곡의 노래책 데이터도 일정한 응답 속도로 조회 가능.
- **버그 근절**: 데이터 필드명 불일치로 인한 UI 오류 완전 해결.
- **확장성**: 향후 룰렛 시스템과의 통합(노래책에서 룰렛 항목 자동 생성 등) 기반 마련.

---
*작성일: 2026-03-25, Senior Full-Stack AI Partner 물멍 작성*
