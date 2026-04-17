# Priority 2: 복잡도를 줄이세요 — Task List

## Phase 1: Contracts 경량화 (DbContext 인터페이스 이동)

### 1-1. ISongBookDbContext → Modules.SongBook 내부로 이동
- `[x]` `Modules.SongBook/Abstractions/ISongBookDbContext.cs` 생성
- `[x]` `Contracts/SongBook/Interfaces/ISongBookDbContext.cs` 삭제
- `[x]` `Modules.SongBook` 내부 파일들 using 문 갱신 (~10개)

### 1-2. IRouletteDbContext → Modules.Roulette 내부로 이동
- `[x]` `Modules.Roulette/Abstractions/IRouletteDbContext.cs` 생성
- `[x]` `Contracts/Roulette/Interfaces/IRouletteDbContext.cs` 삭제
- `[x]` `Modules.Roulette` 내부 파일들 using 문 갱신 (~6개)

### 1-3. IPointDbContext → Modules.Point 내부로 이동
- `[x]` `Modules.Point/Abstractions/IPointDbContext.cs` 생성
- `[x]` `Contracts/Point/Interfaces/IPointDbContext.cs` 삭제
- `[x]` `Modules.Point` 내부 파일들 using 문 갱신 (~6개)
- `[x]` `Infrastructure.csproj`에 Modules.Point 참조 추가 (B안)
- `[x]` `Infrastructure/PointWriteBackWorker.cs` using 문 갱신

### 1-4. ICommandDbContext → Modules.Commands 내부로 이동
- `[x]` `Modules.Commands/Abstractions/ICommandDbContext.cs` 생성
- `[x]` `Contracts/Commands/Interfaces/ICommandDbContext.cs` 삭제
- `[x]` `Modules.Commands` 내부 파일들 using 문 갱신 (~8개)

### 1-5. 공통 수정
- `[x]` `Infrastructure.csproj`에 모든 Modules 참조 추가
- `[x]` `Infrastructure/Persistence/AppDbContext.cs` using 문 갱신
- `[x]` `Infrastructure/DependencyInjection.cs` using 문 갱신
- `[x]` `dotnet build` 검증

## Phase 2: AppDbContext OnModelCreating 분리

- `[x]` `Infrastructure/Persistence/Configurations/` 디렉토리 생성
- `[x]` `CoreEntityConfigurations.cs` 작성
- `[x]` `SongBookEntityConfigurations.cs` 작성
- `[x]` `RouletteEntityConfigurations.cs` 작성
- `[x]` `PointEntityConfigurations.cs` 작성
- `[x]` `CommandEntityConfigurations.cs` 작성
- `[x]` `OverlayEntityConfigurations.cs` 작성
- `[x]` `PhilosophyEntityConfigurations.cs` 작성
- `[x]` `LedgerEntityConfigurations.cs` 작성
- `[x]` `AppDbContext.OnModelCreating` 정리 (490줄 → ~40줄)
- `[x]` 중복 ToTable() 정리
- `[x]` `dotnet build` 검증

## Phase 3: WorkerRegistry.cs 통합

- `[x]` `Infrastructure/Workers/WorkerRegistry.cs` 생성
- `[x]` `Application/DependencyInjection.cs` — 워커 등록 코드 제거
- `[x]` `Infrastructure/DependencyInjection.cs` — 워커 등록 코드 제거
- `[x]` `Modules.Roulette/DependencyInjection.cs` — 워커 등록 코드 제거
- `[x]` `Api/Program.cs` 또는 호출 지점에서 `AddBotEngineWorkers()` 호출 확인
- `[x]` `dotnet build` 검증
