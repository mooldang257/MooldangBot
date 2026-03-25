# 📚 노래책(Songbook) 시스템 구현 결과 보고서 (Final Plan)

본 문서는 2026-03-25에 완료된 노래책 시스템 구축 및 오버레이 버그 수정에 대한 최종 기술 명세를 기록합니다.

---

## 1. 개요 (Overview)

노래책(Songbook)은 스트리머가 부를 수 있는 방대한 곡 목록을 효율적으로 관리하고, 시청자의 신청곡 대기열(SongQueue)과 연동하는 핵심 관리 모듈입니다.

- **목표**: 대규모(1,000곡 이상) 곡 목록의 무중단 조회 및 관리.
- **핵심 기술**: ASP.NET 10, EF Core(MariaDB), Seek Pagination(인풋 페이징), SignalR.

---

## 2. 데이터베이스 설계 (Database Schema)

### 2-1. `SongBook` 테이블
`LastId` 기반의 성능 최적화(Index Seek)를 위해 `(ChzzkUid, Id DESC)` 복합 인덱스를 적용했습니다.

| 필드명 | 타입 | 제약 조건 | 설명 |
|---|---|---|---|
| `Id` | `int` | PK, AI | 고유 식별자 |
| `ChzzkUid` | `string` | Required, Index | 스트리머 식별자 (테넌트 격리) |
| `Title` | `string` | Required | 노래 제목 (PascalCase) |
| `Artist` | `string` | Nullable | 가수/아티스트 명 |
| `UsageCount` | `int` | Default: 0 | 총 신청 횟수 통계 |
| `IsActive` | `bool` | Default: true | 신청 가능 활성화 상태 |

---

## 3. API 엔드포인트 세부 명세

### 3-1. 노래책 인풋 페이징 조회
- **URL**: `GET /api/songbook/{chzzkUid}`
- **파라미터**:
  - `LastId`: 선택 (이전 페이지의 마지막 ID).
  - `PageSize`: 선택 (기본 20).
  - `Search`: 선택 (제목/가수 통합 검색 키워드).
- **특징**: `WHERE Id < LastId` 조건을 사용하여 데이터 양에 관계없이 일정한 조회 성능 유지.

### 3-2. 대기열(SongQueue) 연동
- **URL**: `POST /api/songbook/{chzzkUid}/add-to-queue/{id}`
- **로직**:
  1. 노래책에서 곡 정보 조회.
  2. `SongQueue`의 마지막 `SortOrder`를 확인하여 새로운 항목 추가.
  3. 노래책의 `UsageCount`를 1 증가시켜 통계 데이터 갱신.

---

## 4. 오버레이 버그 수정 내역 (`undefined` 해결)

- **원인**: 백엔드 PascalCase(C#) 전송 규격과 프론트엔드 lowercase(JS) 참조 불일치.
- **조치**: 
  - `songlist_overlay.html`, `songlist.html`의 모든 필드 접근 방식을 `song.Title`, `song.Artist` 등 PascalCase로 통일.
  - 전역 JSON 정책(`PropertyNamingPolicy = null`)과의 데이터 정합성 100% 확보.

---

## 5. UI/UX 구현 사항

### 5-1. `admin_songbook.html` (관리 페이지)
- **무한 리스트**: "더 보기" 버튼을 통한 인풋 페이징 UI 구현.
- **통합 검색**: 제목과 가수를 서버에서 실시간으로 검색하여 렌더링.
- **CRUD 모달**: 페이지 새로고침 없이 곡 정보를 관리할 수 있는 AJAX 기반 모달 폼.

### 5-2. `main.html` (대시보드)
- 카드 형식의 메뉴 링크를 추가하여 노래책 관리에 즉시 접근 가능하도록 구성.

---
*최종 업데이트: 2026-03-25, Senior Full-Stack Partner '물멍'*
