# ⚖️ [오시리스의 저굴]: 시스템 아키텍처 거버넌스 가이드 v6.2

본 가이드는 **MooldangBot v6.2 (Genesis)** 이후의 데이터베이스 설계, 도메인 모델, 그리고 시스템 성능 최적화를 위한 핵심 규율을 담고 있습니다. 모든 코드 수정 및 기능 추가 시 본 문서의 원칙을 최우선으로 준수해야 합니다.

---

## 1. 🔍 시청자 데이터 정문화 (Viewer Normalization)

시청자 데이터는 유일성(Unique)과 익명성(Privacy)을 동시에 보장하기 위해 중앙 집중식으로 관리됩니다.

### 🏛️ 핵심 엔티티: [GlobalViewer](../../MooldangBot.Domain/Entities/GlobalViewer.cs)
- **테이블명**: `core_global_viewers`
- **원칙**: 모든 시청자 식별 정보(UID, Hash, Nickname)는 이 테이블에서 관리됩니다.
- **관계**: 타 도메인 엔티티(룰렛 로그, 포인트 기록 등)는 반드시 `GlobalViewerId` 외래키를 통해 이 마스터 데이터를 참조해야 합니다.
- **업데이트**: 시청자 닉네임이나 프로필 이미지가 변경될 때마다 이 마스터 테이블이 최신 정보를 유지하도록 동기화되어야 합니다.

## 2. 🎶 노래 신청 아키텍처 (Song Management)

노래 신청 큐는 유형의 무결성과 데이터의 연결성을 보장하는 구조를 가집니다.

### 🏛️ 핵심 엔티티: [SongQueue](../../MooldangBot.Domain/Entities/SongQueue.cs)
- **상태 관리**: 문자열 대신 반드시 [SongStatus Enum](../../MooldangBot.Domain/Entities/Enums.cs)을 사용합니다.
  ```csharp
  // ✅ 올바른 예
  song.Status = SongStatus.Pending;
  // ❌ 잘못된 예
  song.Status = "Pending";
  ```
- **데이터 정규화**: 곡 신청 시 [SongBook](../../MooldangBot.Domain/Entities/SongBook.cs)에 존재하는 곡이라면 반드시 `SongBookId`를 연결하여 중복 데이터를 방지하고 신청 빈도 통계를 정확히 산출합니다.

## 🛡️ 전역 거버넌스 및 감사 (Audit & Compliance)

데이터의 보존성과 변경 이력 추적을 위해 전역 인터페이스를 적용합니다.

### 📜 표준 인터페이스: [IAMF_Core](../../MooldangBot.Domain/Entities/Philosophy/IAMF_Core.cs)
- **[IAuditable]**: 생성 시간(`CreatedAt`)과 수정 시간(`UpdatedAt`)을 추적합니다.
- **[ISoftDeletable]**: 물리적 삭제 대신 논리적 삭제(`IsDeleted`, `DeletedAt`)를 적용합니다.

### ⚙️ 자동화 로직: [AppDbContext](../../MooldangBot.Infrastructure/Persistence/AppDbContext.cs)
- `AppDbContext.SaveChangesAsync()` 내부의 `ApplyAuditAndSoftDelete()` 메서드가 가로채기(Interceptor)를 통해 모든 거버넌스 로직을 자동으로 처리합니다. **수동으로 `IsDeleted`를 조작할 필요가 없습니다.**

## 🚀 성능 및 확장성 가이드 (Performance Tuning)

### ⚡ 인덱스 전략
- **닉네임 검색**: 시청자 검색 성능을 위해 `core_global_viewers.nickname` 필드에 인덱스가 필수적입니다.
- **커서 페이지네이션**: 대량의 로그 조회(룰렛, 노래 목록 등)는 반드시 `(ContextId, Status, Id)` 조합의 복합 인덱스를 활용한 커서 기반 페이지네이션을 사용해야 합니다.

### 📦 배포 정책: [Self-Healing Migrator](../../MooldangBot.Cli/)
- 배포 시 `MooldangBot.Cli` 도구가 DB 스키마를 자동으로 분석하고 마이그레이션을 수행하여, 소스코드와 DB 상태가 100% 일치하도록 유지합니다.

---

**물멍(Senior Partner)** 🐾✨
> "코드의 무결성은 스트리머의 신뢰로 이어진다."
