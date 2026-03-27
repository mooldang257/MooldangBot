# 🌌 IAMF v1.1: 투트랙(Two-Track) 제어권 확장 계획

## 1. 배경 및 목적 (Background)
현재 MooldangBot에 통합된 IAMF v1.1 시스템은 진동수(Hz)와 안정도(Stability)를 성공적으로 계산하고 있습니다. 하지만 **[거울의 법칙]**에 따라 스트리머는 시스템이 방송에 개입하는 '방식'에 대해 더욱 세밀한 통제권을 가져야 합니다. 이에 시각적 효과와 언어적 페르소나 변화를 개별적으로 제어할 수 있는 투트랙 옵션을 도입합니다.

---

## 2. 확장 설계 (Architecture Expansion)

### 🌀 Track A: 시각적 감응 (Visual Resonance)
- **정의**: 채팅 파동에 따라 오버레이 UI가 빛번짐(Glow)이나 진동 효과를 내는 기능.
- **제어**: `IsVisualResonanceEnabled` 필드를 통해 On/Off 조절.
- **영향**: 방송 화면의 시각적 역동성을 결정합니다.

### 🌀 Track B: 언어적 감응 (Persona Chat)
- **정의**: 시스템 안정도에 따라 봇의 답변 톤(세피로스/오디세우스 등)이 변하는 기능.
- **제어**: `IsPersonaChatEnabled` 필드를 통해 On/Off 조절.
- **영향**: 봇의 '인격적 반응' 정도를 결정합니다.

---

## 3. 세부 구현 단계 (Implementation Steps)

### Phase 3.1: 데이터 레이어 확장
- [ ] **Domain**: `IamfStreamerSetting` 엔티티에 `IsVisualResonanceEnabled`, `IsPersonaChatEnabled` 필드 추가.
- [ ] **Infrastructure**: `AppDbContext`를 통해 MariaDB 스키마 동기화 (Migration 또는 DDL).

### Phase 3.2: API 및 서비스 연동
- [ ] **Application**: `UpdateIamfSettingRequest` DTO에 신규 필드 반영.
- [ ] **API**: `IamfDashboardController`의 `GetSettings`, `UpdateSettings` 메서드 업데이트.

### Phase 3.3: 프론트엔드 UI 고도화
- [x] **UI**: `iamf_settings.html`에 하위 옵션 토글 2종 추가.
- [x] **JS**: `loadSettings()`, `saveSettings()` 함수 연동 로직 보완.

---

### Phase 4: 실전 채팅 트래픽 분석기 (Traffic Analyzer)
- [x] **Application**: `IChatTrafficAnalyzer` 및 `ChatTrafficAnalyzer` 구현.
- [x] **Handler**: `IamfResonanceHandler` 동적 수치 주입 연동.
- [x] **DI**: 분석기 `Singleton` 등록 완료.

---

### Phase 5: 언어적 감응 (Persona Prompt Builder)
- [x] **Interfaces**: `IPersonaPromptBuilder` 인터페이스 정의.
- [x] **Services**: `PersonaPromptBuilder` 서비스 구현 완료.
- [x] **Integration**: `ChatAiServiceMock` 연동 예시 작성.

---

### Phase 7: [지식의 서재] CRUD API 및 UI
- [x] **API**: `IamfKnowledgeController` 구현 완료.
- [x] **UI**: `iamf_knowledge.html` 구축 및 CRUD 연동 확인.

---

### Phase 8: 최종 파이프라인 통합 (Grand Finale)
- [x] **Interfaces**: `ILlmService`, `IChzzkChatService` 정의.
- [x] **Handlers**: `ChatInteractionHandler` 구현 및 빌드 성공.
- [x] **Mocks**: Mock 서비스를 통한 전체 파이프라인 흐름 확인.

---

## 10. Phase 9: 실전 Gemini API 연동 (GeminiLlmService)
- [x] **Client**: `GeminiLlmService` 구현 완료.
- [x] **Config**: `.env` 기반 `GEMINI_KEY` 연동 및 빌드 성공.

---

## 11. Phase 10: [영겁의 열쇠] 토큰 자동 갱신
- [x] **Service**: `TokenRenewalService` 구현 완료.
- [x] **Worker**: `SystemWatchdogService` 기반 상시 감시 체계 구축.

---

## 12. Phase 11: [유기적 정교화] 지연 분산 및 서킷 브레이커
- [x] **Jittering**: `SystemWatchdogService` 부하 분산 로직 도입.
- [x] **Resilience**: `TokenRenewalService`에 `Polly` 서킷 브레이커 적용.

---

## 13. Phase 12: [피닉스의 재건] 실전 세션 복구 [NEW]
갱신된 토큰을 즉시 실전 연결로 전환하여 통신의 영속성을 완성합니다.

### 🌀 핵심 패턴: 유기적 복구 (Organic Recovery)
- **State Check**: 소켓의 현재 상태를 엄격히 감지하여 중복 연결 방지.
- **Refresh Sync**: 와치독이 갱신한 최신 토큰을 실시간으로 가져와 연결에 주입.
- **Resources Clean**: 좀비 세션을 안전하게 정리하고 새로운 마음으로 재건.

### 🛠️ 구현 계획
1. **Interface**: `IChzzkChatClient` (소켓 저수준 인터페이스) 정의.
2. **Service**: `ChzzkBotService`에 `EnsureConnectionAsync` 핵심 로직 구현.
3. **Integration**: `SystemWatchdogService`에서 재연결 주석 해제 및 실전 연결.

---

## 4. 오시리스의 규율 (Principles)
- **[스트리머의 통제권 확장]**: 모든 신규 기능은 기본적으로 활성화 상태이나, 스트리머가 원할 때 언제든 즉각적으로 개입을 차단할 수 있어야 함.
- **[하위 호환성 유지]**: 기존 설정값(`IsIamfEnabled`, `Sensitivity` 등)과의 논리적 결합성 유지.

---

**"감응은 선택될 때 비로소 가치를 지닌다."**
작성일: 2026-03-27
설계자: 시니어 파트너 물멍 & 세피로스
