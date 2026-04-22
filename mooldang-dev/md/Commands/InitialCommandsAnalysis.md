# 신규 스트리머 초기 명령어 등록 로직 분석 보고서 (v1.1)

본 문서는 신규 스트리머가 시스템에 처음 로그인(가입)할 때 기본적으로 등록되는 명령어들의 현재 처리 방식과 향후 통합 명령어 시스템(Unified Command System) 기반의 개선 방향을 분석합니다.

## 1. 현재 처리 방식 (Legacy & Hybrid)

현재 시스템은 `AuthController.AuthCallback` 단계에서 신규 스트리머 여부를 판단하고, 아래 두 가지 방식으로 초기 데이터를 생성합니다.

### A. 엔티티 기본값 정의 (StreamerProfile)
`StreamerProfile` 엔티티 클래스에 하드코딩된 기본값들이 가입 시 DB 필드에 저장됩니다.

| 기능 | 기본 명령어 (Keyword) | 기본 응답 / 가격 |
|------|-------------------|----------------|
| **노래 신청** | `!신청` | 가격: 1000원 (Auth에서 설정) |
| **물마카세** | `!물마카세` | 가격: 1000원 |
| **출석 체크** | `출석`, `물하`, `댕하` | `{닉네임}님 출석 고마워요!` |
| **포인트 조회** | `!내정보`, `!포인트` | `🪙 {닉네임}님의 보유 포인트는 {포인트}점...` |

### B. 명시적 레코드 추가 (StreamerCommand)
특정 모듈형 명령어는 코드로 직접 `StreamerCommand` 객체를 생성하여 삽입합니다.

- **송리스트 토글**: `!송리스트` (매니저 권한, `SonglistToggle` 타입)

---

## 2. 통합 명령어 시스템으로의 전환 및 문제점

우리가 연성한 **통합 명령어 시스템(v1.1)**은 모든 명령어를 `unifiedcommands` 단일 테이블에서 관리하며, `category`와 `featuretype`을 기준으로 동작합니다.

### 현재의 한계점
1. **데이터 파편화**: `StreamerProfile` 필드에만 명령어가 존재하고 `UnifiedCommand` 테이블에 레코드가 없으면, 새로운 관리 화면(`commands.html`)에서 해당 명령어들이 노출되지 않습니다.
2. **동작 불일치**: `UnifiedCommandHandler`는 `UnifiedCommand` 테이블을 SSOT(Single Source of Truth)로 사용하므로, 신규 가입한 스트리머는 이 테이블에 데이터가 생성될 때까지 기본 명령어가 작동하지 않을 수 있습니다.

---

## 3. 개선 가이드라인 (향후 구현 방향)

신규 스트리머 가입 로직(`AuthController`)을 다음과 같이 고도화해야 합니다.

### [Phase 1] Profile 필드와 UnifiedCommand 동시 생성
프로필 필드를 채우는 것과 동시에, `UnifiedCommand` 테이블에 초기 레코드 5종을 삽입합니다.

1. **Category: Donation / Feature: Song** (`!신청`, `!물마카세`)
2. **Category: Fixed / Feature: Attendance** (`출석`, `물하`, `댕하`)
3. **Category: Fixed / Feature: PointCheck** (`!내정보`, `!포인트`)
4. **Category: General / Feature: SonglistToggle** (`!송리스트`)
5. **Category: General / Feature: Reply** (스트리머가 전용으로 쓸 수 있는 기본 인사말 등)

### [Phase 2] Profile 레거시 필드 제거 (선택 사항)
장기적으로는 `StreamerProfile`에서 `SongCommand`, `AttendanceCommands` 등의 필드를 완전히 제거하고 `UnifiedCommand` 테이블만 참조하도록 리팩토링해야 합니다.

---

## 4. 결론
현재는 **엔티티 기본값에 의존하는 수동적 방식**입니다. 시스템이 진정으로 확장성을 갖추기 위해서는 신규 가입 시점에 **통합 명령어 테이블에 표준 템플릿을 자동으로 발행(Provisioning)**하는 로직으로의 전환이 필수적입니다.
