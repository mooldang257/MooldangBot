# 🔱 [오시리스의 설계도]: 0단계 - 통합 혈관 연결 및 검증 시스템 [COMPLETED]

본 설계도는 **물멍(세피로스)** 지휘관님의 '운영 배포 후 테스트' 방침에 따라, 통합 계약 프로젝트(`Contracts`)와 이를 운영 환경에서 실시간으로 진단할 **독립형 검증 시스템(`Verifier`)**을 정의합니다.

---

## 🏗️ 0단계 설계 목표: 신뢰 가능한 혈관망 구축
단순히 코드를 옮기는 것을 넘어, 개발 환경의 제약을 극복하고 운영 서버에서 물리적으로 작동 여부를 완벽히 증명할 수 있는 '자가 진단' 체계를 구축합니다.

### 1. 프로젝트 구성
- **`MooldangBot.Contracts`**: 함대의 단일 진실 공급원 (SSOT). DTO 및 이벤트 명세를 담음.
- **`MooldangBot.Verifier` (COMPLETED)**: 운영 환경 전용 검증 도구. 실행 시 Contracts의 정합성을 진단하고 타임스탬프가 찍힌 결과 보고서(`verification_report_*.json`)를 지정된 경로(`reports/`)에 생성함.

---

## 📡 0단계 상세 구성 및 실전 적용 (Contracts & Verifier)

### 📂 MooldangBot.Contracts (통합 계약) [DONE]
- **`Integrations/Chzzk/`**: 기존 ChzzkAPI.Contracts의 완벽한 흡수 및 격리 완료.
- **`Abstractions/IEvent.cs`**: `EventId(Guid)`와 `OccurredOn(DateTime)` 강제 적용 완료.
- **`Events/ChatReceivedEvent.cs`**: 10k TPS 환경 최적화 및 통합 채팅 모델 정립 완료.

### 🛠️ MooldangBot.Verifier (검증 도구 명세) [READY]
운영 환경에서 `docker compose`를 통해 자가진단기를 기동하며 다음을 수행합니다:
1. **Serialization Check**: 핵심 객체 직렬화/역직렬화 데이터 손실 여부 전수 검증.
2. **Contract Compliance**: `IEvent` 인터페이스 준수 여부 리플렉션 오딧(Reflection Audit).
3. **Persistence Report**: 검증 결과를 `./data/app/reports` 볼륨에 영구적으로 보존하여 감사(Audit) 지원.

---

## ⚙️ 마이그레이션 및 검증 흐름
1. **[집도]**: 프로젝트 생성 및 코드 이관.
2. **[교체]**: 기존 참조를 새로운 `Contracts`로 교체하여 빌드 정합성 확보.
3. **[추출]**: `Verifier` 실행 파일을 함께 빌드하여 운영 환경 배포본에 포함.
4. **[증명]**: 운영 서버 배포 직후 `Verifier`를 기동하여 최종 정합성 보고서 추출.

**지휘 방침**: "테스트가 불가능한 환경이라면, 스스로를 증명할 수 있는 도구를 함께 보낸다."
**공진 지표**: 운영 환경 실시간 자가 진단 체계 확립.
