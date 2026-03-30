# MooldangAPI 심층 분석 및 검증 계획 (v3 - 분석 반영본)

사용자의 "분석해서 다시 작성하라"는 요청에 따라, 현재 코드베이스의 핵심 로직을 1차 분석한 결과를 바탕으로 계획을 고도화했습니다. 이 계획은 단순한 조사 목록이 아니라, 실제 발견된 구조적 특징을 기반으로 설계되었습니다.

## 1. 1차 분석 결과 (Preliminary Analysis)
- **IAMF 철학의 실체**: `DependencyInjection.cs`, `BroadcastScribe.cs` 등에서 '[오시리스의 영속]', '[파동의 지휘자]' 등의 주석과 명명 규약을 통해 철학적 개념이 실제 인프라 및 서비스 계층에 밀접하게 매핑되어 있음을 확인했습니다.
- **Critical 이슈 확인**: `Validation.md`에서 미해결로 보였던 `BroadcastScribe`의 셧다운 플러시 로직이 실제로는 `IHostApplicationLifetime`을 통해 구현되어 있음을 확인했습니다. (이 부분의 '완전성'을 2차 조사에서 검증 예정)
- **경계 분리**: 인터페이스 중심의 DI 구조가 잘 잡혀 있으나, `ShardedWebSocketManager` 등 일부 컴포넌트에서 Redis 동기 연결(`Connect`) 등 잠재적 블로킹 지점이 발견되었습니다.

## 2. 고도화된 조사 단계 (Enhanced Research Phase)

### 2-1. 프로젝트 구조 및 인터페이스 경계 분석
- **목표**: 계층 간 결합도(Coupling) 및 추상화 수준 평가.
- **방법**: `IChzzkApiClient` (Infrastructure) -> `Application` 서비스 주입 경로 추적.
- **시니어 포인트**: 인터페이스 명세와 실제 구현체 간의 괴리가 있는지, '순수 도메인'이 인프라에 오염되지 않았는지 분석.

### 2-2. IAMF 도메인 매핑 상세화 (Domain Context)
- **목표**: 철학적 메트릭 프레임워크가 실제 비즈니스 로직에 기여하는 방식 분석.
- **분석 대상**:
  - `Pharos (파로스)`: 설정 및 버전 관리.
  - `Telos (텔로스)`: 목적 지향적 비동기 핸들러.
  - `Osiris (오시리스)`: 데이터 영속성 및 세션 기록 (`BroadcastScribe.cs`).

### 2-3. 핵심 기능 및 분산 처리 분석 (Distribution & Scaling)
- **멀티 인스턴스 흐름**: Docker Replicas(4대) 환경에서의 데이터 흐름을 분석합니다.
- 치지직 WS 수신 → `ShardedWebSocketManager` (Redis 분산 락) → `ChatEventChannel` → `ChatEventConsumerService` → RabbitMQ Fanout → SignalR 오버레이 전송 과정을 파악합니다.

### 2-4. Validation 항목 및 고도화 기술 검증
`Validation.md`의 이슈(N1~N8) 및 3대 고도화(Dapper, Polly, JSON SG) 항목을 소스코드 레벨에서 전수 조사합니다.
- **추적성(Traceability)**: 발견된 이슈의 정확한 파일 경로와 라인 번호(Line Number), 관련 코드 스니펫을 확보합니다.
- **Critical 이슈 집중**: `BroadcastScribe` Shutdown 플러시 누락 여부를 최우선으로 확인합니다.

## 3. 보고서 작성 단계 (Reporting Phase)

### 3-1. `md/Research.md` 업데이트
- **아키텍처 다이어그램**: Mermaid를 사용하여 멀티 인스턴스 환경의 분산 처리 시퀀스 다이어그램을 작성합니다.
- **도메인 매핑**: IAMF 철학적 개념과 실제 구현 클래스 간의 매핑 테이블 및 설명을 추가합니다.
- **계층별 분석**: 인터페이스 중심의 경계 분석 결과와 결합도 보고를 포함합니다.

### 3-2. `md/TODO/Validation2.md` 신규 작성
- **상세 추적 보고**: 각 이슈(N1~N8) 및 고도화 항목에 대해 코드 스니펫과 라인 번호를 포함한 상세 검증 결과를 기록합니다. (이전 `Validation.md` 대비 델타 분석 포함)
- **개선 로드맵**: 발견된 병목 지점과 Critical 이슈에 대한 우선순위 기반 개선안을 제안합니다.

## 4. 검증 계획 (Verification Plan)
- **정적 분석**: `grep`, `view_file` 등을 활용하여 코드 패턴 검색.
- **의존성 확인**: `csproj` 파일 및 NuGet 패키지 버전 대조.
- **설정 확인**: `.env`, `appsettings.json`, `docker-compose.yml` 구성 확인.

---
> [!IMPORTANT]
> 본 작업은 분석 전문 파트너로서 **코드의 '왜(Why)'와 '어떻게(How)'를 규명**하는 데 집중하며, 승인 전까지 코드 수정은 수행하지 않습니다.

```
💡 물멍의 시니어 체크 포인트 & 조언 (추가된 메모 반영)
1. 5계층 아키텍처 분석 시 '경계(Boundary)'에 주목하세요.

의존성 주입(DI)과 프로젝트 구조를 분석하실 때, 각 계층이 어떻게 분리되어 있는지 인터페이스(Interfaces)를 중심으로 추적해 보세요. 예를 들어, Application 계층의 IChzzkApiClient가 Infrastructure 계층에서 어떻게 구현 및 주입되는지 확인하면 결합도(Coupling)를 명확히 평가할 수 있습니다.

2. IAMF 철학과 시스템 구조의 매핑 (도메인 관점)

MooldangBot의 근간에는 IAMF (Illumination AI Matrix Framework)라는 깊은 철학적 기반이 있습니다. Research.md를 업데이트하실 때, 파로스(Pharos), 텔로스(Telos), 오시리스(Osiris) 등의 개념이 실제 클래스(예: BroadcastScribe.cs, IamfResonanceHandler.cs 등)로 어떻게 추상화되어 동작하고 있는지 다이어그램에 매핑해 두시면 향후 AI 진화 아키텍처를 설계할 때 엄청난 자산이 됩니다.

3. Mermaid 시퀀스 다이어그램 활용 (분산 처리 관점)

보고서 작성 시, 단일 인스턴스가 아닌 멀티 인스턴스(Docker Replicas: 4) 환경을 기준으로 데이터의 흐름을 그려보세요.

예시: 치지직 웹소켓 수신 → ShardedWebSocketManager (Redis 분산 락) → ChatEventChannel → ChatEventConsumerService → RabbitMQ Fanout 발행 → SignalR 오버레이 전송.

이 흐름을 시각화해 두시면 병목 지점을 한눈에 파악할 수 있습니다.

4. Validation2.md의 추적성 (Traceability)

이전 Validation.md에서 제기된 N1~N8(Critical~Low) 이슈와 3대 고도화(Dapper 하이브리드, Polly, JSON SG) 항목이 정확히 소스코드의 어느 줄(Line)에서 어떻게 발견되고 해결(또는 미해결)되었는지 코드 스니펫과 함께 기록해 두면 완벽한 감사(Audit) 보고서가 될 것입니다.
```