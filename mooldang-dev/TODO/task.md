# 📑 MooldangBot 아키텍처 고도화 작업 현황 (Task)

## 🏗️ Priority 2: 리팩터링 및 복잡도 감소

### [x] Phase 1: Contracts 프로젝트 경량화
- [x] 모듈별 인터페이스 이동 (`ISongBookDbContext`, `IRouletteDbContext`, `IPointDbContext`, `ICommandDbContext` → 각 모듈 `Abstractions` 폴더)
- [x] 모듈 내 소스 코드 using 문 일괄 갱신
- [x] `Contracts` 프로젝트 내 구버전 인터페이스 파일 삭제
- [x] `Infrastructure` 프로젝트 설정 동기화 및 빌드 안정성 검증

### [x] Phase 2: AppDbContext 분리 (OnModelCreating 해체)
- [x] `Infrastructure/Persistence/Configurations/` 디렉토리 구조 생성
- [x] 엔티티별 `IEntityTypeConfiguration<T>` 구현 파일 생성 (SongBook, Point, Roulette, Commands 등 8개 도메인)
- [x] `AppDbContext.cs` 리팩터링 및 `ApplyConfigurationsFromAssembly` 도입
- [x] 정합성 및 빌드 성공 확인

### [/] Phase 3: 워커 등록 통합 (WorkerRegistry.cs)
- [ ] `Infrastructure/Workers/WorkerRegistry.cs` 생성
- [ ] `Application` 및 `Infrastructure`에 흩어진 15+개 `HostedService` 이동
- [ ] 주기를 설정 파일(`appsettings.json`)과 연동하여 중앙 제어화
- [ ] 기존 DI 파일에서 개별 워커 등록 코드 제거 및 `AddWorkerRegistry()` 호출로 통합

### [ ] Phase 4: 대형 도메인(SongBook) 적출
- [ ] `MooldangBot.Application` 내 `SongBook` 관련 로직 식별
- [ ] `MooldangBot.Modules.SongBook` 프로젝트로 이관 및 격리
- [ ] 의존성 구조 최적화 및 빌드 검증

---
> **물멍의 일지**: 복잡했던 AppDbContext를 성공적으로 찢어내고 Configurations 폴더로 정렬을 마쳤습니다. 이제 함선의 비대한 Application 엔진실에서 SongBook이라는 무거운 부품을 도려내어 전용 모듈로 독립시킬 때가 다가오고 있습니다.
