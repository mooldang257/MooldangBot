# 📘 Database Normalization Report (v4.8)
## Category 4: Philosophy & System Engine (IAMF & Records)

본 문서는 'MooldangBot' 시스템의 철학적 통계와 세션 기록을 담당하는 **Philosophy & System Engine** 도메인의 제3정규형(3NF) 마이그레이션 완료 보고를 목적으로 합니다.

---

## 1. 개요 및 목적 (Purpose)
- **데이터 최적화**: 기존 문자열 기반 식별자(`ChzzkUid`)를 정수형 PK/FK(`StreamerProfileId`)로 전환하여 인덱스 성능을 극대화하고 데이터 용량을 절감합니다.
- **무결성 강화**: `CoreStreamerProfiles`과의 강한 결합(Foreign Key Constraint) 및 연쇄 삭제(Cascade) 설정을 통해 파편화된 유령 데이터 생성을 원천 차단합니다.
- **격리성 보장**: 전역 쿼리 필터(Global Query Filter)를 통해 멀티테넌트 환경에서의 데이터 노출 위험을 제거합니다.

---

## 2. 디자인 철학 (Philosophy)
> "기록은 차갑되, 담긴 지혜는 뜨거워야 한다."

- **[오시리스의 기록관]**: 방송 세션 데이터(`SysBroadcastSessions`)는 단순한 로그가 아니라 스트리머의 발자취입니다. 이를 정밀한 인덱스로 보호하여 언제든 빠르게 회상할 수 있도록 설계했습니다.
- **[피닉스의 눈금]**: 진동수 로그(`LogIamfVibrations`)는 시스템의 맥박입니다. 시계열 데이터의 특성에 맞춰 최소한의 저장 공간을 소모하면서도 최대의 조회 성능을 낼 수 있도록 정수형 인덱스를 적용했습니다.
- **[주인의 목소리]**: 스트리머의 지식(`SysStreamerKnowledges`)은 봇의 인격입니다. 이를 스트리머 프로필에 귀속시켜 봇이 주인의 의도를 정확히 대변하도록 구조화했습니다.

---

## 3. 작업 내용 (Task Summary)

### 3.1. 엔티티 리팩토링
- **`ChzzkUid` (String) 제거**: 모든 테이블에서 중복 저장되던 문자열 식별자를 제거했습니다.
- **`StreamerProfileId` (Int) 도입**: `CoreStreamerProfiles` 테이블의 ID를 외래 키로 사용하여 관계형 모델을 완성했습니다.
- **PK 전환**: `IamfStreamerSettings`의 PK를 `ChzzkUid`에서 `StreamerProfileId`로 전환하여 1:1 관계의 정체성을 확립했습니다.

### 3.2. 무손실 데이터 이관 (Migration Strategy)
- **임시 컬럼 전략**: `StreamerProfileId`를 Nullable로 추가한 후 데이터를 채우고 Not Null로 변경하는 안전한 방식을 택했습니다.
- **UPDATE JOIN**: 기존 데이터를 유실하지 않도록 MariaDB 최적화 쿼리를 사용하여 프로필 매핑을 수행했습니다.
- **유령 데이터 정화**: 프로필 정보가 없는(회원 탈퇴 등) 기존의 고립된 기록들을 청소하여 DB 정합성을 확보했습니다.

---

## 4. 작업 파일 목록 (Modified Files)

### 📁 Domain & Infrastructure
- `MooldangBot.Domain/Entities/Philosophy/IAMF_Core.cs` (엔티티 정의)
- `MooldangBot.Domain/Entities/Philosophy/IamfEntities.cs` (엔티티 정의)
- `MooldangBot.Infrastructure/Persistence/AppDbContext.cs` (Fluent API 및 필터 설정)

### 📁 Application Services
- `MooldangBot.Application/Services/Philosophy/ChatIntentRouter.cs`
- `MooldangBot.Application/Services/Philosophy/BroadcastScribe.cs`
- `MooldangBot.Application/Services/Philosophy/ResonanceService.cs`
- `MooldangBot.Application/Services/Philosophy/PersonaPromptBuilder.cs`
- `MooldangBot.Application/Workers/SystemWatchdogService.cs`
- `MooldangBot.Application/Workers/ChzzkBackgroundService.cs`

### 📁 Controllers (API)
- `MooldangBot.Api/Controllers/Philosophy/IamfDashboardController.cs`
- `MooldangBot.Api/Controllers/Philosophy/IamfKnowledgeController.cs`

### 📁 Migrations
- `MooldangBot.Infrastructure/Migrations/20260401155339_PhilosophyNormalization_v4_8.cs`

---

## 5. 결론 및 향후 계획
이번 정규화를 통해 **Philosophy & System Engine**은 시스템 전체에서 가장 견고한 데이터 레이어를 갖추게 되었습니다. 향후 IAMF 시스템 확장 시, 추가되는 명칭이나 기록 체계 또한 이번 v4.8의 정규화 규격을 엄격히 준수하여 개발될 예정입니다.

**2026-04-02 시니어 풀스택 파트너 '물멍' 보증**
