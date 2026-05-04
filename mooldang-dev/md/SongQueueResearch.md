# 🎵 신청곡 및 노래책(Songbook) 시스템 상세 분석 보고서

> 작성일: 2026-03-25  
> 분석자: 물멍 (Senior Full-Stack AI Partner)

---

## 1. 시스템 개요

신청곡 시스템은 시청자가 채팅 명령어 또는 후원을 통해 곡을 신청하고, 스트리머가 실시간으로 대기열을 관리하는 핵심 기능입니다. 2026-03-25 패치를 통해 대규모 곡 목록 관리를 위한 **노래책(Songbook)** 기능이 추가되도록 설계되었습니다.

**데이터 흐름:**

```
신청 (시청자/스트리머)       관리 (대시보드)           송출 (오버레이)
────────────────         ──────────────         ────────────────
채팅 !신청          ──▶    FuncSongListQueues        ──▶   songlist_overlay
치즈 후원           ──▶    (Active Session) ──▶   (PascalCase Sync)
노래책에서 추가      ──▶
```

---

## 2. 관련 파일 목록

| 파일 | 역할 |
|------|------|
| `Controllers/SongController.cs` | 신청곡 CRUD 및 상태 변경 (Pending/Playing/Completed) |
| `Controllers/SonglistController.cs` | 신청곡 세션 및 오버레이용 통합 데이터 제공 |
| `Controllers/SongBookController.cs` | **[NEW]** 노래책(전체 레퍼토리) CRUD 및 인풋 페이징 |
| `Models/FuncSongListQueues.cs` | 현재 활성 대기열 엔터티 |
| `Models/FuncSongBooks.cs` | **[NEW]** 스트리머 전체 곡 목록 엔터티 |
| `wwwroot/songlist.html` | 스트리머 대시보드 UI |
| `wwwroot/songlist_overlay.html` | OBS용 실시간 송리스트 오버레이 |
| `wwwroot/admin_songbook.html` | **[NEW]** 노래책 관리 및 인풋 페이징 UI |

---

## 3. 핵심 아키텍처 및 기술적 의사결정

### 3-1. 데이터 정합성: PascalCase 통합 (Bug Fix)
- **현상**: 오버레이에서 곡 정보가 `undefined`로 표시됨.
- **원인**: 백엔드(`Program.cs`)에서 `PropertyNamingPolicy = null`로 설정되어 데이터가 PascalCase로 전송되나, 프론트엔드에서는 lowercase 필드(`song.title`)에 접근함.
- **해결**: 모든 프론트엔드 속성 참조를 PascalCase(`song.Title`)로 통일하여 런타임 오류 방지.

### 3-2. 성능 최적화: 인풋 페이징 (Seek Pagination)
- **배경**: 노래책에 수천 곡 이상의 데이터가 쌓일 경우 `OFFSET` 방식은 DB 성능 저하를 유발함.
- **설계**: `LastId` 파라미터를 활용하여 `WHERE Id < LastId` 기반의 페이징을 수행함으로써 데이터 양에 관계없이 일정한 조회 성능(O(log N)) 유지.
- **UI**: "더 보기" 버튼 또는 무한 스크롤 형태의 인풋 페이징 인터페이스 도입.

---

## 4. 데이터 모델 설계

### 4-1. `FuncSongBooks` (노래책 엔터티)
| 필드명 | 설명 | 비고 |
|---|---|---|
| `Id` | 고유 식별자 | PK, 인덱스 |
| `ChzzkUid` | 스트리머 식별자 | 멀티테넌트 격리 |
| `Title` | 곡 제목 | 필수 |
| `Artist` | 가수 이름 | - |
| `IsActive` | 신청 가능 여부 | 기본값 true |
| `UsageCount` | 총 신청 횟수 | 통계용 |

---

## 5. 향후 확장 계획
1. **노래책 검색**: 제목/가수 기반의 실시간 서버 사이드 검색 기능.
2. **자동 동기화**: `FuncSongListQueues`에서 완료된 곡을 자동으로 노래책에 등록하거나 카운트를 증가시키는 로직.
3. **엑셀 반입/반출**: 대량의 노래책 데이터를 관리하기 위한 CSV/Excel 인터페이스.

---
*이 보고서는 MooldangBot 신청곡 및 노래책 시스템의 설계를 위해 작성되었습니다.*  
*분석 기준: 2026-03-25, 물멍(AI) 작성*
