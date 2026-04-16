# Priority 2: 복잡도를 줄이세요 — Task List

## Phase 1: Contracts 경량화 (DbContext 인터페이스 이동)

### 1-1. ISongBookDbContext → Modules.SongBook 내부로 이동
- `[ ]` `Modules.SongBook/Abstractions/ISongBookDbContext.cs` 생성
- `[ ]` `Contracts/SongBook/Interfaces/ISongBookDbContext.cs` 삭제
- `[ ]` `Modules.SongBook` 내부 파일들 using 문 갱신 (~10개)

### 1-2. IRouletteDbContext → Modules.Roulette 내부로 이동
- `[ ]` `Modules.Roulette/Abstractions/IRouletteDbContext.cs` 생성
- `[ ]` `Contracts/Roulette/Interfaces/IRouletteDbContext.cs` 삭제
- `[ ]` `Modules.Roulette` 내부 파일들 using 문 갱신 (~6개)

### 1-3. IPointDbContext → Modules.Point 내부로 이동
- `[ ]` `Modules.Point/Abstractions/IPointDbContext.cs` 생성
- `[ ]` `Contracts/Point/Interfaces/IPointDbContext.cs` 삭제
- `[ ]` `Modules.Point` 내부 파일들 using 문 갱신 (~6개)
- `[ ]` `Infrastructure.csproj`에 Modules.Point 참조 추가 (B안)
- `[ ]` `Infrastructure/PointWriteBackWorker.cs` using 문 갱신

### 1-4. ICommandDbContext → Modules.Commands 내부로 이동
- `[ ]` `Modules.Commands/Abstractions/ICommandDbContext.cs` 생성
- `[ ]` `Contracts/Commands/Interfaces/ICommandDbContext.cs` 삭제
- `[ ]` `Modules.Commands` 내부 파일들 using 문 갱신 (~8개)

### 1-5. 공통 수정
- `[ ]` `Infrastructure.csproj`에 모든 Modules 참조 추가
- `[ ]` `Infrastructure/Persistence/AppDbContext.cs` using 문 갱신
- `[ ]` `Infrastructure/DependencyInjection.cs` using 문 갱신
- `[ ]` `dotnet build` 검증

## Phase 2: AppDbContext OnModelCreating 분리

- `[ ]` `Infrastructure/Persistence/Configurations/` 디렉토리 생성
- `[ ]` `CoreEntityConfigurations.cs` 작성
- `[ ]` `SongBookEntityConfigurations.cs` 작성
- `[ ]` `RouletteEntityConfigurations.cs` 작성
- `[ ]` `PointEntityConfigurations.cs` 작성
- `[ ]` `CommandEntityConfigurations.cs` 작성
- `[ ]` `OverlayEntityConfigurations.cs` 작성
- `[ ]` `PhilosophyEntityConfigurations.cs` 작성
- `[ ]` `LedgerEntityConfigurations.cs` 작성
- `[ ]` `AppDbContext.OnModelCreating` 정리 (490줄 → ~40줄)
- `[ ]` 중복 ToTable() 정리
- `[ ]` `dotnet build` 검증

## Phase 3: WorkerRegistry.cs 통합

- `[ ]` `Infrastructure/Workers/WorkerRegistry.cs` 생성
- `[ ]` `Application/DependencyInjection.cs` — 워커 등록 코드 제거
- `[ ]` `Infrastructure/DependencyInjection.cs` — 워커 등록 코드 제거
- `[ ]` `Modules.Roulette/DependencyInjection.cs` — 워커 등록 코드 제거
- `[ ]` `Api/Program.cs` 또는 호출 지점에서 `AddBotEngineWorkers()` 호출 확인
- `[ ]` `dotnet build` 검증
