# MooldangBot 잔존 이슈 해결 및 아키텍처 고도화 Task

- [x] **Step 1: 시스템 종료 안전성 보장 (Graceful Shutdown & 비동기 제어)**
    - [x] [N1] BroadcastScribe 종료 시 `_activeStats` DB 플러시 로직 구현
    - [x] [N3, N4] WebSocket 관리 계층(Manager/Shard) 동기 블로킹 `Dispose` 제거
    - [x] [#10] ChzzkChatService, RouletteService 내 `CancellationToken.None` 교체
- [x] **Step 2: 회복 탄력성(Polly) 및 객체 수명 주기(DI) 교정**
    - [x] [12-2] ChzzkApiClient 전역 `AddStandardResilienceHandler` 적용
    - [x] [N5] ChzzkBackgroundService Captive Dependency 문제 수정 (ScopeFactory 도입)
    - [x] [M3] Redis 연결 로직 비동기(ConnectAsync) 또는 Lazy 패턴으로 전환
- [x] **Step 3: DB I/O 최적화 (Dapper 하이브리드 및 N+1 제거)**
    - [x] [12-1] PointTransactionService Dapper 기반 Atomic UPDATE 도입
    - [x] [#6, N6] PeriodicMessageWorker N+1 쿼리 최적화 및 UTC 시간대 통일
- [x] **Step 4: 백그라운드 동시성 제어 및 컨슈머 확장**
    - [x] [#7] Watchdog 및 BackgroundService 재진입 방지(SemaphoreSlim) 적용
    - [x] [M1] ChatEventConsumerService 병렬 소비자 수 상향 (3 -> 8)
- [x] **Step 5: 코드 품질 정리 및 SignalR 그룹 라우팅 완성**
    - [x] [10-2] 프로젝트 전반 Structured Logging(구조화된 로깅) 템플릿 적용
    - [x] [N2] OverlayHub 연결 시 쿼리 스트링 기반 자동 그룹 가입 구현

- [x] **Step 6: Docker 배포 환경 고도화 및 MariaDB 전환**
    - [x] [.env.sample] 명시적 저장 경로를 포함한 환경 변수 템플릿 작성
    - [x] [docker-compose.yml] MariaDB 전환 및 Bind Mount 경로 최적화 (헬스체크 및 비밀번호 파싱 교정)
    - [x] [deploy.sh] 빌드 체크 및 헬스 체크 로직 강화
    - [x] [Dockerfile/csproj] EF Bundle 빌드 오류 및 패키지 충돌(NU1605) 해결
    - [x] [.dockerignore] data/ 권한 거부(Permission Denied) 이슈 해결
    - [x] [backup.sh] 자동 DB 백업 스크립트 작성 및 Cron 가이드 추가
