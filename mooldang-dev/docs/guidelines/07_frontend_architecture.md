# 07. Frontend Architecture (Studio)

이 가이드라인은 `MooldangBot.Studio`(SvelteKit 기반 프론트엔드)가 대규모(Enterprise-scale)로 확장되더라도 길을 잃지 않도록 돕는 **'도메인/기능 중심 구조 (Feature-Based Architecture)'**의 핵심 원칙을 정의합니다.

## 1. 핵심 철학 (Core Philosophy)
- **Studio & Viewer 분리:** 레이아웃 그룹 `(viewer)`와 `(streamer)`를 사용하여 시청자와 스트리머의 디자인 시스템 및 권한 레이어를 물리적으로 분리합니다.
- **동적 라우팅 (`[streamerId]`):** 모든 스트리머 관련 주소는 `/[streamerId]` 파라미터를 포함하여 개인화된 URL 환경을 제공합니다.
- **관심사의 완벽한 분리:** `routes` 폴더는 조립(Factory) 역할만 하며, 실제 비즈니스 로직은 `$lib/features` 내부에 격리합니다.

---

## 2. 공식 폴더 구조 체계 (Directory Map)

```text
📦 src
 ┣ 📂 routes                # 📍 [라우팅 & 페이지 조립] UI의 진입점
 ┃ ┣ 📂 (viewer)            # 👥 시청자용 레이아웃 그룹 (Public)
 ┃ ┃ ┣ 📂 [streamerId]
 ┃ ┃ ┃ ┣ 📂 songbook        # 노래책 조회/검색
 ┃ ┃ ┃ ┗ 📂 roulette        # 룰렛 실행
 ┃ ┃ ┗ 📜 +layout.svelte    # 시청자용 감성 UI 레이아웃
 ┃ ┃
 ┃ ┣ 📂 (streamer)          # 👑 스트리머용 레이아웃 그룹 (Auth Required)
 ┃ ┃ ┣ 📂 [streamerId]
 ┃ ┃ ┃ ┗ 📂 dashboard       # 스트리머 스튜디오 (명령어/노래책 설정)
 ┃ ┃ ┗ 📜 +layout.svelte    # 스트리머용 관리 UI 레이아웃
 ┃ ┃
 ┃ ┣ 📜 +page.svelte        # 메인 랜딩 (시청 중인 스트리머 찾기)
 ┃ ┣ 📜 +layout.svelte      # 전역 공통 설정 (GSAP, Tailwind 등)
 ┃ ┗ 📜 +error.svelte       # 커스텀 에러 페이지 (길을 잃은 물댕이)
 ┃
 ┣ 📂 lib                   # 🛠️ [실제 비즈니스 로직 및 컴포넌트] ($lib)
 ┃ ┣ 📂 core                # ⚙️ [공통 코어] 함선 어디서나 쓰는 범용 부품
 ┃ ┃ ┗ 📂 state             # 🌐 [전역 상태] Svelte 5 Runes 기반 전역 엔진 (userState 등)
 ┃ ┗ 📂 features            # 🎯 [도메인 기능] 특정 목적을 가진 독립 모듈
```

---

## 3. 계층별 역할 및 라우팅 규칙

### 3.1. `streamerId` 동적 파라미터 활용
- 페이지 내부에서 링크 이동 시 반드시 `$page.params.streamerId`를 포함해야 합니다.
- 예: `<a href="/{$page.params.streamerId}/dashboard/cmd">`

### 3.2. `hooks.server.ts` 인증 가드
- 모든 `dashboard` 포함 경로는 세션 쿠키를 검증해야 합니다.
- `streamerId`는 대소문자 구분을 위해 자동으로 `toLowerCase()` 리다이렉트 처리됩니다.

### 3.3. 전역 상태 관리 (Bridge Global State)
- **Engine**: Svelte 5 Runes (`$state`, `$derived`) 기반.
- **Location**: `$lib/core/state/`
- **Principle**: `userState`와 같은 싱글톤 인스턴스를 사용하여 Prop Drilling을 방지합니다.
- **Security**: 전역 상태는 **UI 표시용**으로만 신뢰하며, 민감한 비즈니스 로직 및 인증 검증은 반드시 백엔드(C#) 세션 기반으로 수행합니다.

### 3.4. 에러 처리
- `+error.svelte`는 `status === 404` 시 활성화된 스트리머 목록을 추천하여 사용자 이탈을 방지합니다.

---

## 4. Studio 8.0 안정화 기술 스택 (Stable Tech Stack)

> [!CAUTION]
> **[버전 고정 및 변경 금지 규정]**  
> 아래 명시된 버전들은 `MooldangBot.Studio` 프로젝트의 아키텍처 정합성과 **Vite 5 <-> Svelte Plugin 3** 간의 전처리기 호환성을 위해 정밀하게 튜닝되었습니다. **사용자의 명시적 요청 없이 버전을 변경할 경우 '500 Mapping Error' 및 빌드 크래시가 발생할 수 있으므로 절대 임의 수정을 금지합니다.**

### 💎 공식 엔진 사양 (Standard Engines)
- **UI Framework**: `svelte: ^5.0.0` (Runes v5.55.1+ Only)
- **Code Style**:
    - **레거시 금지**: `export let` 대신 `$props()` 사용 권장.
    - **반응성**: 단순 데이터는 `$state`, 계산된 값은 `$derived`, 부수 효과는 `$effect` 적극 활용.
    - **ID 할당**: 모든 상호작용 요소(버튼, 입력 폼 등)는 고유한 `id`를 부여하여 브라우저 테스트 및 접근성을 보장합니다.
- **Vite Engine**: `^5.2.0` (Stability-First)
- **Style Engine**: `tailwindcss: ^4.0.0` (@tailwindcss/vite Native Mode)
- **Animation Engine**: `gsap: ^3.14.2` (Ultra Stable)

### ⚠️ 버전 변경 시 발생 가능한 현상
- **500 Internal Error**: `transformWithEsbuild` 누락으로 인한 컴파일 실패.
- **Dependency Conflict**: Svelte 4/5 라이브러리 간의 피어 디펜던시 충돌.
- **Node/NPM Instability**: Node 24 이상의 환경에서 발생하는 의존성 인식 거부 현상.
