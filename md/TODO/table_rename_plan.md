# [Plan] 도메인 및 기능별 테이블 명칭 전면 개정

본 문서는 프로젝트의 테이블 명칭을 도메인 및 기능 단위로 그룹화하여 재정의하기 위한 상세 실행 계획서입니다.

## 1. 확정된 명칭 규칙 (Naming Convention)

| 도메인 그룹 | 접두사 | 설명 |
| :--- | :--- | :--- |
| **CORE** | `core_` | 서비스 운영을 위한 필수 핵심 인프라 데이터 |
| **VIEW** | `view_` | 시청자 활동 및 상호작용 관련 데이터 |
| **SONG_BOOK** | `song_book_` | 스트리머가 관리하는 라이브러리(곡 저장소) 데이터 |
| **SONG_LIST** | `song_list_` | 실시간 신청곡 현황 및 방송 세션 데이터 |
| **FUNC** | `func_` | 독립적인 기능 모듈 (룰렛, 명령어, 오마카세 등) |
| **OVERLAY** | `overlay_` | 방송 송출용 소스 및 출사 연출 설정 |
| **SYS** | `sys_` | 시스템 전역 설정 및 인프라 캐시 데이터 |
| **IAMF** | `iamf_` | 존재 보존 철학 도메인 (기존 유지) |

## 2. 상세 테이블 매핑 리스트

| 구분 | 현재 테이블명 | 변경 후 테이블명 | 비고 |
| :--- | :--- | :--- | :--- |
| **Core** | `streamer_profiles` | `core_streamer_profiles` | |
| | `global_viewers` | `core_global_viewers` | |
| | `streamer_managers` | `core_streamer_managers` | |
| **View** | `viewer_profiles` | `view_profiles` | |
| | `periodic_messages` | `view_periodic_messages` | |
| **Song (Book)** | `song_books` | `song_book_main` | **송북 분리** |
| **Song (List)** | `song_queues` | `song_list_queues` | **송리스트 분리** |
| | `song_list_sessions` | `song_list_sessions` | |
| **Func (Roulette)** | `roulettes` | `func_roulette_main` | **기능성 func_ 적용** |
| | `roulette_items` | `func_roulette_items` | |
| | `roulette_logs` | `func_roulette_logs` | |
| | `roulette_spins` | `func_roulette_spins` | |
| **Func (Omakase)** | `streamer_omakases` | `func_omakase_items` | |
| **Func (Cmd)** | `unified_commands` | `func_cmd_unified` | |
| | `master_command_categories` | `func_cmd_master_categories` | |
| | `master_command_features` | `func_cmd_master_features` | |
| | `master_dynamic_variables` | `func_cmd_master_variables` | |
| **Overlay** | `overlay_presets` | `overlay_presets` | |
| | `shared_components` | `overlay_components` | |
| | `avatar_settings` | `overlay_avatar_settings` | |
| **System** | `system_settings` | `sys_settings` | |
| | `broadcast_sessions` | `sys_broadcast_sessions` | |
| | `chzzk_categories` | `sys_chzzk_categories` | |
| | `chzzk_category_aliases` | `sys_chzzk_category_aliases` | |

## 3. 실행 단계 (Action Items)

### Step 1: AppDbContext 매핑 수정
- `OnModelCreating` 내의 각 엔티티별 `ToTable()` 명칭을 위 표에 맞춰 변경합니다.
- `func_cmd_master_variables` 시딩 쿼리 내의 하드코딩된 테이블명을 전수 교체합니다.

### Step 2: 소스 코드 내 SQL 쿼리 동기화
- `Dapper` 및 `Raw SQL`을 사용하는 모든 파일에서 예전 테이블명을 검색하여 일괄 변경합니다.

### Step 3: 데이터베이스 마이그레이션 적용
- `dotnet ef migrations add Revision_v6_2_DomainPrefixRenaming`
- 생성된 마이그레이션 파일의 `RenameTable` 로직을 최종 검수 후 `Update-Database` 실행.

## 4. 주의사항 (Caution)
> [!CAUTION]
> 테이블 명칭 변경은 데이터베이스 마이그레이션 시 기존 데이터를 안전하게 보존하기 위해 `RenameTable` 명령을 사용해야 합니다. 직접 테이블을 삭제하고 새로 만드는 현상이 발생하지 않도록 마이그레이션 파일을 꼼꼼히 확인해야 합니다.
