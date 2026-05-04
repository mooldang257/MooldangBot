# MooldangBot.Presentation 모듈 상세 분석 보고서

## 1. 개요
`MooldangBot.Presentation`은 외부 사용자 및 클라이언트(OBS, 대시보드)와 직접 상호작용하는 계층입니다. 일반적인 계층형 아키텍처와 달리 **수직적 피처 구조(Vertical Slice)**를 채택하여 기능별로 컨트롤러와 관련 로직이 응집되어 있습니다.

---

## 2. 수직적 피처 구조 (Vertical Slice)
컨트롤러가 하나의 폴더에 모여있지 않고, `Features/` 하위의 각 기능 폴더 내에 위치합니다.
- **Features/Auth/AuthController**: 치지직 OAuth 2.0 인증, 세션 쿠키 발급, RBAC 처리를 담당합니다.
- **Features/Admin/AdminBotController**: 스트리머별 봇 설정 및 마스터 관리 기능을 담당합니다.
- **Features/FuncSongListQueues/SongController**: 신청곡 추가, 삭제 및 현재 큐 조회 기능을 제공합니다.
- **Features/FuncRouletteMain/RouletteController**: 룰렛 설정 및 결과 로그 조회를 담당합니다.

---

## 3. 실시간 통신 (Hubs)

### 3-1. OverlayHub (SignalR)
- **그룹 관리**: `JoinStreamerGroup`을 통해 OBS 오버레이 클라이언트를 스트리머 고유 UID 그룹으로 묶어 관리합니다.
- **상태 동기화**: `UpdateOverlayState`, `UpdateOverlayStyle` 메서드를 통해 대시보드에서 설정한 디자인이나 상태 변화를 실시간으로 모든 오버레이 클라이언트에 브로드캐스트합니다.
- **프리셋 지원**: `JoinPresetGroup`을 통해 특정 디자인 프리셋별 독립적인 업데이트를 지원합니다.

---

## 4. 보안 및 인증 (Security)

### 4-1. OAuth 인증 플로우
- `AuthController`의 `callback` 액션에서 치지직 토큰 교환 후, 사용자의 UID를 확인하여 `master`, `manager`, `streamer` 역할을 부여합니다.
- 사용자가 관리 권한을 가진 모든 채널 ID를 `AllowedChannelId` 클레임으로 저장하여 멀티 채널 관리를 지원합니다.

### 4-2. 정책 기반 권한 부여 (Policy-Based)
- **ChannelManagerAuthorizationHandler**: `[Authorize(Policy = "ChannelManager")]` 속성이 붙은 엔드포인트 호출 시, 현재 사용자가 해당 채널(`chzzkUid`)에 대한 권한이 있는지 라우트 데이터와 클레임을 대조하여 검증합니다.

---

## 5. 특징적인 로직
- **이미지 프록시**: `ProxyImage` 엔드포인트를 통해 치지직/네이버의 프로필 이미지를 CORS 문제 없이 오버레이에서 표시할 수 있도록 중계 인터페이스를 제공합니다.
- **Zero-Git 대응**: `BASE_DOMAIN` 설정을 환경 변수에서 로드하여 다중 인스턴스 배포 환경에서의 리다이렉트 URL 정합성을 보장합니다.

---
*분석 완료 - 2026-03-27 물멍(AI)*
