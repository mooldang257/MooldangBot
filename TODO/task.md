# 📑 MooldangBot 아키텍처 고도화 작업 현황 (Task)

- [x] **Phase 1: Contracts & Base Infrastructure**
    - [x] Phase 1: Contracts 프로젝트 경량화
    - [x] 모듈별 인터페이스 이동 (`Abstractions` 폴더 생성)
    - [x] 소스 코드 내 using 문 일괄 갱신
    - [x] `Infrastructure` 프로젝트 설정 변경 (모듈 참조 추가)
    - [x] 빌드 테스트 및 컴파일 오류 해결 (Dapper 버전 및 모호성 해결)

- [/] **Phase 2: AppDbContext 분리 (IEntityTypeConfiguration)**
    - [ ] `Infrastructure/Persistence/Configurations/` 하위 폴더 구성
    - [ ] 모듈별 Configuration 파일 생성 (Point, SongBook, Roulette, Commands 등)
    - [ ] `AppDbContext.OnModelCreating` 리팩터링 및 중복 `ToTable()` 정리

- [ ] **Phase 3: WorkerRegistry 통합 (HostedService)**
    - [x] `UnifiedCommandHandler` 로직 리팩토링 (선결제 후실행 파이프라인 도입)
    - [x] `HandleCompensationAsync` (치즈 복구 Saga) 로직 구현
    - [x] 에러 피드백 메세지 고도화

- [x] **Phase 4: Verification & Polish**
    - [x] 동시성 테스트 (Race Condition 검증)
    - [x] 보상 트랜잭션 정상 작동 확인
    - [x] 최종 코드 정리 및 주석 보강

- [x] **Phase 5: 10k TPS Optimization & Reliability**
    - [x] `MySqlBulkCopy` 기반 초고속 로그 적재 시스템 구현
    - [x] `ChzzkJsonContext` 기반 Zero-Allocation 직렬화 적용
    - [x] 핫패스(핫패스) 핵심 경로 테스트 커버리지 강화 (23건 통과)
    - [x] 아키텍처 10k TPS 한계점 및 유지보수성 진단 보고서 작성
