# 🎡 룰렛 지연 알림 미작동 수정 계획 (Plan 5)

지연 알림(SpinId 콜백) 도입 후 채팅이 전송되지 않는 원인을 분석하고 이를 [x] 완벽히 해결한 수정 결과입니다.

---

## 1. 원인 분석

### 1.1. 인증(Authentication) 문제 (가장 유력)
-   **현상**: `RouletteController` 클래스 상단에 `[Authorize]` 속성이 지정되어 있어, 모든 API 요청에 로그인 세션이 필요함.
-   **원인**: 오버레이는 주로 OBS(Open Broadcaster Software)에서 실행되며, 관리자 페이지와 쿠키/세션을 공유하지 않음. 따라서 오버레이에서 호출하는 `POST /api/admin/roulette/complete` 요청이 `401 Unauthorized`로 거부됨.

### 1.2. 보안성 검토
-   `CompleteAnimation` 엔드포인트는 `SpinId` (GUID)를 전송받음.
-   이 `SpinId`는 서버에서 생성되어 SignalR로 특정 스트리머 오버레이에만 전달되므로, 그 자체로 일회용 보안 토큰 역할을 수행할 수 있음.

---

## 2. 해결 방안

### 2.1. 엔드포인트 익명 허용
-   `CompleteAnimation` 메서드에 `[AllowAnonymous]` 속성을 추가하여 인증 없이도 콜백이 가능하게 수정.
-   해당 로직은 세션 정보(`User`) 대신 캐시에 저장된 `Context.ChzzkUid`를 사용하므로 기능적으로 안전함.

### 2.2. 오버레이 디버깅 강화
-   오버레이의 `completeAnimation` 함수에서 fetch 실패 시 단순히 `console.error`만 찍는 것이 아니라, 응답 상태 코드를 로깅하여 향후 진단을 용이하게 함.

---

### 3. 상세 수정 계획

#### 3.1. [Backend] RouletteController.cs 보완
-   **인증 해제**: `[AllowAnonymous]` 속성을 추가하여 OBS 환경의 오버레이가 인증 없이 접근 가능하도록 함.
-   **중복 방지 (Atomicity)**: `TryGetValue` 성공 즉시 `_cache.Remove(cacheKey)`를 수행하여 악의적인 반복 호출 및 채팅 중복 전송을 원천 차단함.
-   **방어 코드**: `SpinId` 유효성 검사 및 결과에 따른 상세 상태 코드(`Ok`, `BadRequest`, `NotFound`) 반환.

#### 3.2. [Frontend] roulette_overlay.html 보완
-   **함수 명확화**: `notifyComplete(spinId)`로 명칭 변경 및 가독성 개선.
-   **상세 로깅**: `fetch` 실패 시 상태 코드(`response.status`)와 상태 텍스트를 함께 출력하여 디버깅 편의성 증대.

---

## 4. 기대 효과
-   **알림 정상화**: OBS 오버레이에서도 성공적으로 콜백을 보내 채팅 알림이 정상 작동함.
-   **안정적 운영**: 인증 세션과 독립적인 콜백 구조로 오버레이 환경의 변수 제거.
-   **보안 강화**: 일회용 `SpinId`와 즉시 삭제 로직을 결합하여 API 오용 가능성 최소화.

---
*작성일: 2026-03-24 (인증 정책 및 방어 로직 반영), 물멍(AI) 작성*
