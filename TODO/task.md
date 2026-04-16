# 📑 MooldangBot 아키텍처 고도화 작업 현황 (Task)

## 🏗️ Priority 2: 리팩터링 및 복잡도 감소

### [x] Phase 1: Contracts 프로젝트 경량화
- [x] 모듈별 인터페이스 이동 (`ISongBookDbContext`, `IRouletteDbContext`, `IPointDbContext`, `ICommandDbContext` → 각 모듈 `Abstractions` 폴더)
- [x] 모듈 내 소스 코드 using 문 일괄 갱신 (SongBook, Roulette, Point, Commands 모듈 25+개 파일)
- [x] `Contracts` 프로젝트 내 구버전 인터페이스 파일 삭제 (중복 및 모호성 제거)
- [x] `Infrastructure` 프로젝트 설정 동기화
    - [x] `Infrastructure.csproj`에 모듈 프로젝트 참조 추가
    - [x] `AppDbContext.cs` using 문 및 인터페이스 구현 갱신
    - [x] `DependencyInjection.cs` 서비스 등록 갱신
- [x] 빌드 안정성 검증
    - [x] `Dapper` 패키지 버전 충돌 해결 (2.1.35 → 2.1.72)
    - [x] 중복/모호한 인터페이스 참조 오류 수정
    - [x] 전체 솔루션 빌드 성공 확인

### [/] Phase 2: AppDbContext 분리 (OnModelCreating 해체)
- [ ] `Infrastructure/Persistence/Configurations/` 디렉토리 구조 생성
- [ ] 엔티티별 `IEntityTypeConfiguration<T>` 구현 파일 생성
    - [ ] SongBook 관련 (SongBook, Omakase 등)
    - [ ] Point 관련 (ViewerPoint, Donation 등)
    - [ ] Roulette 관련 (Roulette, RouletteItem 등)
    - [ ] Commands 관련 (UnifiedCommand 등)
- [ ] `AppDbContext.cs` 리팩터링
    - [ ] 기존 490줄의 Fluent API 코드 제거
    - [ ] `ApplyConfigurationsFromAssembly` 도입
    - [ ] 중복 `ToTable()` 호출 및 매핑 최적화
- [ ] 모델 무결성 검증 (`dotnet ef dbcontext optimize` 또는 빌드 확인)

### [ ] Phase 3: 워커 등록 통합 (WorkerRegistry.cs)
- [ ] `Infrastructure/Workers/WorkerRegistry.cs` 생성
- [ ] `Application` 및 `Infrastructure`에 흩어진 15+개 `HostedService` 이동
- [ ] 주기를 설정 파일(`appsettings.json`)과 연동하여 중앙 제어화
- [ ] 기존 DI 파일에서 개별 워커 등록 코드 제거 및 `AddWorkerRegistry()` 호출로 통합

---
> **물멍의 일지**: Phase 1의 인터페이스 이동으로 인해 Contracts와 Modules 간의 의존성 구조가 훨씬 깔끔해졌습니다. 이제 비대한 AppDbContext를 찢어서 유지보수성을 극대화할 준비가 되었습니다.
