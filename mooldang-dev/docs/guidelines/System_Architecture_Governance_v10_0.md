# ⚖️ [오시리스의 저굴]: 시스템 아키텍처 거버넌스 가이드 v10.0

본 가이드는 **MooldangBot v10.0 (Osiris-Prime)** 도입과 프로젝트 구조 단순화(19개 → 9개) 이후의 데이터베이스 설계, 도메인 모델, 그리고 시스템 성능 최적화를 위한 핵심 규율을 담고 있습니다.

---

## 1. 🔍 시청자 데이터 정문화 (Viewer Normalization)

시청자 데이터는 유일성(Unique)과 익명성(Privacy)을 동시에 보장하기 위해 중앙 집중식으로 관리됩니다.

### 🏛️ 핵심 엔티티: [CoreGlobalViewers](../../MooldangBot.Domain/Entities/CoreGlobalViewers.cs)
- **테이블명**: `core_global_viewers`
- **관리 원칙**: 모든 시청자 식별 정보는 `MooldangBot.Domain`의 엔티티에서 관리됩니다. 타 도메인은 반드시 `GlobalViewerId`를 통해 마스터 데이터를 참조해야 합니다.

## 2. 🛡️ 전역 거버넌스 및 감사 (Audit & Compliance)

### ⚙️ 자동화 로직: [AppDbContext](../../MooldangBot.Infrastructure/Persistence/AppDbContext.cs)
- `AppDbContext`는 이제 `partial class`로 분리되어 관리됩니다.
- 도메인별 설정은 `Configurations/` 폴더 내의 전용 클래스들(`CoreConfig`, `PointConfig` 등)에서 담당하여 단일 컨텍스트의 비대화를 방지합니다.

## 🚀 성능 및 확장성 가이드 (Performance Tuning)

### ⚡ 10k TPS 대응 전략 (architecture_review.md 반영)
- **배치 처리**: `BoundedChannel<T>`과 `BatchWorker`를 활용하여 초당 수만 건의 I/O를 효율적으로 처리합니다.
- **JSON Source Generation**: 모든 통신 DTO는 **[ChzzkJsonContext](../../MooldangBot.Domain/Contracts/Chzzk/ChzzkJsonContext.cs)**에 등록하여 런타임 오버헤드를 제거해야 합니다.

### 📦 운영 도구 실행: [Self-Healing Tools](../../MooldangBot.Cli/)
- `Cli`, `Verifier` 등의 도구는 이제 솔루션 외부에서 관리됩니다. 실행 시 다음과 같은 방식을 사용하십시오.
  - `dotnet run --project MooldangBot.Cli`
  - `dotnet run --project MooldangBot.Verifier`

## 3. 🛡️ 카오스 엔지니어링 및 탄력성 (Chaos & Resilience)

### 🌀 핵심 서비스 가이드
- **[ChaosManager](../../MooldangBot.Domain/Common/Services/ChaosManager.cs)**: 함대의 시련을 관장하며, 반드시 **전역 싱글톤**으로 작동해야 합니다.
- **[IdempotencyService](../../MooldangBot.Domain/Common/Services/IdempotencyService.cs)**: 중복 요청 방지를 위해 Redis 기반으로 작동하며, `Domain` 레이어의 보편적 계약으로 승격되었습니다.

### 🛡️ 심연의 복원력: 익산 보험 (Iksan Insurance)
- 프로세스 비정상 종료 시, 메모리 버퍼를 로컬 파일로 즉시 덤프하고 기동 시 복구하는 패턴을 필수 적용합니다.

## 4. ⚓ 외부 연동 및 안정성 가이드 (External Guard)

- **Snake Case SQL**: Dapper 등을 활용한 Raw SQL 작성 시, 모든 컬럼명은 반드시 소문자 **snake_case**를 사용합니다. (Docker 환경 호환성 보장)
- **Wait Policy**: 외부 API 호출 시 MassTransit의 재시도 정책 및 서킷 브레이커를 적극 활용하여 시스템 연쇄 붕괴를 하드닝합니다.

---

**물멍(Senior Partner)** 🐾✨
> "구조의 단순함이 곧 성능의 극대화이며, 규율의 준수가 곧 함대의 생존이다."
