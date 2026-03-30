
🏗️ [설계 문서] 잔존 이슈 해결 및 아키텍처 고도화 작업 명세서
작업은 의존성과 위험도를 고려하여 5개의 논리적 Step으로 분리했습니다. 미니에게 한 번에 하나의 Step씩 지시하여 코드의 정합성을 유지하는 것이 좋습니다.

📍 Step 1: 시스템 종료 안전성 보장 (Graceful Shutdown & 비동기 제어)
대상 이슈: N1(Critical), N3, N4, 기존 #10

N1 해결: BroadcastScribe에 IAsyncDisposable을 구현하거나 IHostApplicationLifetime을 주입받아, 서버 종료(ApplicationStopping) 시 _activeStats 메모리 딕셔너리에 남은 채팅 통계 데이터를 DB에 강제 플러시(Flush)하는 로직 추가.

N3, N4 해결: ShardedWebSocketManager와 WebSocketShard의 Dispose() 내에 있는 동기 블로킹(GetAwaiter().GetResult()) 코드를 제거하고, 비동기 해제 로직으로 완전 통합.

#10 해결: ChzzkChatService 및 RouletteService 내부의 CancellationToken.None을 호출 컨텍스트의 토큰이나 시스템 종료 토큰으로 교체.

📍 Step 2: 회복 탄력성(Polly) 및 객체 수명 주기(DI) 교정
대상 이슈: 12-2(P1), N5, M3

12-2 해결: DependencyInjection.cs의 AddHttpClient<IChzzkApiClient, ChzzkApiClient>() 체인에 .AddStandardResilienceHandler()를 추가하여 모든 외부 API 호출에 재시도 및 서킷 브레이커 일괄 적용 (.NET 8+ 표준 기능).

N5 해결 (Captive Dependency): Singleton인 ChzzkBackgroundService 생성자에서 Transient인 IChzzkApiClient를 직접 주입받는 것을 제거. 대신 IServiceScopeFactory를 주입받아 실행 루프 내에서 Scope를 생성하여 Client를 Resolve하도록 수정.

M3 해결: DependencyInjection.cs의 Redis 설정부에서 ConnectionMultiplexer.Connect() 동기 호출을 ConnectionMultiplexer.ConnectAsync() 대기 구조 또는 Lazy<Task<IConnectionMultiplexer>> 패턴으로 변경.

📍 Step 3: DB I/O 최적화 (Dapper 하이브리드 및 N+1 제거)
대상 이슈: 12-1(P1), 기존 #6, N6

12-1 해결: PointTransactionService.AddPointsAsync()의 Read-Modify-Write EF Core 패턴을 제거하고, Dapper를 활용한 단일 Atomic UPDATE 쿼리로 변경하여 동시성 충돌(DbUpdateConcurrencyException) 원천 차단.

#6 & N6 해결: PeriodicMessageWorker의 N+1 쿼리 루프를 EF Core의 Include() 또는 배치 조회를 활용해 단일 쿼리로 최적화. 동시에 DateTime.Now를 DateTimeOffset.UtcNow로 변경하여 서버 타임존 독립성 확보.

📍 Step 4: 백그라운드 동시성 제어 및 컨슈머 확장
대상 이슈: 기존 #7, M1

#7 해결: SystemWatchdogService 및 ChzzkBackgroundService의 타이머 루프 진입점에 SemaphoreSlim(1, 1)을 배치하여, 이전 주기의 처리가 길어질 경우 다음 주기가 중첩 실행되는 현상 방지.

M1 해결: ChatEventConsumerService의 ConsumerCount를 현재 3에서 5(또는 8)로 상향하여 피크 타임 메시지 역압(Backpressure) 처리량 증가.

📍 Step 5: 코드 품질 정리 및 SignalR 그룹 라우팅 완성
대상 이슈: 10-2(로깅), N2, N7

10-2 해결: 프로젝트 전반에 남아있는 18건의 문자열 보간($"...") 로깅을 Serilog의 구조화된 로깅 템플릿(Structured Logging) 형식(_logger.LogInformation("... {UserId}", userId);)으로 전면 교체.

N2 해결: OverlayHub.OnConnectedAsync() 내부에서 Context.GetHttpContext().Request.Query["chzzkUid"]를 파싱하여, 클라이언트의 수동 호출 없이도 서버 단에서 즉시 Groups.AddToGroupAsync()를 실행하도록 개선.

N7 해결: Infrastructure.csproj 파일에서 불필요한 Microsoft.Extensions.Diagnostics.HealthChecks 패키지 참조 라인 삭제.