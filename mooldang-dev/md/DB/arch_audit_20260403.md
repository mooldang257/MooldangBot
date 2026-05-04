# ⚖️ [오시리스의 저울]: 시스템 아키텍처 정밀 감사 보고서 v1.0

본 보고서는 **MooldangBot v6.2 (Genesis)** 기반의 데이터베이스 스키마 및 도메인 모델에 대한 전수 조사 결과와 향후 고도화 방향을 담고 있습니다.

## 1. 🔍 시청자 데이터 정화 (Viewer Normalization)

### ✅ 현재 상태 (Current State)
- **중앙화 성공**: 시청자의 고유 UID(암호화), 해시, 닉네임, 프로필 이미지 URL이 `core_global_viewers` 테이블로 완벽하게 통합되었습니다.
- **중복 제거**: `View_StreamerViewer` 테이블에서 닉네임 필드가 제거되고 `GlobalViewerId` 외래키를 통해 마스터 데이터를 참조함으로써 데이터 일관성이 확보되었습니다.

### 💡 개선 제안 (Refinements)
- **인덱스 강화**: 닉네임 검색 빈도가 높을 것으로 예상되므로, `core_global_viewers`의 `nickname` 필드에 비클러스터형 인덱스(Non-clustered Index) 추가를 권장합니다.

## 2. 🎶 노래 신청 체계 (Song Management)

### ⚠️ 발견된 이슈 (Observed Issue)
- **데이터 비연결성**: 현재 `song_list_queues` 테이블은 `song_book_main` 테이블과 직접적인 관계가 없습니다. 제목(Title)과 가수(Artist) 정보가 중복으로 기록되고 있으며, 노래책에 있는 곡을 신청했는지 추적하기 어렵습니다.

### 🛠️ 해결 방안 (Solution)
- **하이브리드 모델**: `FuncSongListQueues` 엔티티에 `SongBookId` (Nullable) 필드를 추가하여, 공식 노래책 기반 신청과 자유 신청을 동시에 수용하면서도 통계적 정확성을 높여야 합니다.

## 3. 🛡️ 상태 관리의 형식화 (State Formalization)

### ⚠️ 발견된 이슈 (Observed Issue)
- **문자열 기반 상태**: `FuncSongListQueues`의 `Status` 필드가 "Pending", "Playing" 등 문자열로 관리되고 있어 런타임 오타에 취약하고 정렬 성능이 낮습니다.

### 🛠️ 해결 방안 (Solution)
- **Enum 전환**: `SongStatus` 열거형을 정의하여 데이터 무결성을 강제하고, DB에서는 이를 정수형(Int)으로 관리하여 색인 성능을 개선해야 합니다.

## 4. 📝 감사 및 거버넌스 (Governance)

### ✅ 현재 상태 (Current State)
- **자동화 로직**: `AppDbContext` 내에 `IAuditable`과 `ISoftDeletable`을 처리하는 강력한 공통 로직이 구현되어 있습니다.

### 💡 개선 제안 (Refinements)
- **누락된 인터페이스**: `SystemSetting`, `CoreGlobalViewers` 등 일부 핵심 테이블에도 물리 삭제 대신 논리 삭제(`ISoftDeletable`)를 적용하여 데이터 보존성을 높여야 합니다.

---

## 📅 감사 일자 및 총평
- **일시**: 2026-04-03
- **총평**: 시스템의 뼈대는 매우 훌륭하며 정규화 수준이 높습니다. 운영 모드로 넘어가기 전 위에서 언급된 **'유형의 무결성(Type Safety)'**만 보강한다면, 마이그레이션 이슈 없는 견고한 시스템이 될 것으로 확신합니다.

**물멍(Senior Partner)** 🐾✨
