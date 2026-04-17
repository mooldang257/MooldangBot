using MooldangBot.Modules.Roulette.Abstractions;
using MooldangBot.Contracts.Roulette.Interfaces;
using MooldangBot.Contracts.Common.Services;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.AI.Interfaces;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.SongBookModule.Abstractions;
using MooldangBot.Modules.Point.Abstractions;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Contracts.Commands.Interfaces;
using Polly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Infrastructure.ApiClients;
using MooldangBot.Infrastructure.Persistence;
// [Migration]: Legacy Messaging namespace removed in favor of MassTransit
using MooldangBot.Infrastructure.ApiClients.Philosophy;
using MooldangBot.Application.Services.Philosophy;
using MooldangBot.Infrastructure.Services;
using StackExchange.Redis;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using RabbitMQ.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MooldangBot.Infrastructure.Security;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using System.Reflection;

namespace MooldangBot.Infrastructure
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // [이지스 파이프라인]: 표준 분산 캐시 인터페이스 등록 (현재는 메모리 기반)
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddSingleton<IIdentityCacheService, IdentityCacheService>();
            services.AddSingleton<INotificationService, NotificationService>();

            // [v2.4.6] 오시리스의 세션: 봇 엔진 등 백그라운드 환경용 기본 세션 등록
            // API 환경에서는 Presentation 레이어에서 등록된 실제 UserSession으로 덮어씌워집니다.
            services.TryAddScoped<IUserSession, BotUserSession>();

            // [v2.4.8] 오시리스의 전령: 백그라운드 환경용 더미 알림 서비스 등록
            // API 환경에서는 Presentation 레이어에서 등록된 실제 OverlayNotificationService로 덮어씌워집니다.
            services.TryAddScoped<IOverlayNotificationService, NullOverlayNotificationService>();

            // [v2.4.7] 수호자의 방패 (Guardian's Shield): 데이터 보호 서비스 등록
            // [물멍]: DB 초기화 스트레스에서 벗어나기 위해 암호화 키를 DB가 아닌 파일 시스템(/root/.aspnet/DataProtection-Keys)에 영속화합니다.
            // Docker 환경에서 해당 경로를 볼륨 매핑하면 DB를 밀어도 기존 토큰 암호화(ChzzkAccessToken 등)가 깨지지 않고 유지됩니다.
            services.AddDataProtection()
                .SetApplicationName("MooldangBot")
                .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"));
            
            // [v13.1] 파로스의 등대: Snowflake 전역 ID 생성기 등록 (Singleton)
            services.AddSingleton<ISongLibraryIdGenerator, SnowflakeIdGenerator>();
            
            // [Phase 9] 심연의 맥박: 건강 모니터링 및 알림용 서비스
            services.AddHttpClient();
            services.AddSingleton<HealthMonitorService>();

            // [오시리스의 영속]: Redis 인프라 구성 (N3/M3: 동기 블로킹 방지 및 지연 연결 보장)
            var redisUrl = configuration["REDIS_URL"]!; // [v22.0] ValidateMandatorySecrets에 의해 존재 보장됨
            
            services.AddSingleton<IConnectionMultiplexer>(sp => 
            {
                var options = ConfigurationOptions.Parse(redisUrl);
                options.AbortOnConnectFail = false; // [핵심] 연결 실패 시에도 즉시 객체를 반환하여 앱 기동 블로킹 방지
                options.ConnectRetry = 5;
                options.ConnectTimeout = 5000;      
                
                // [시니어 팁]: 기동 시점에 동기로 기다리지 않고 백그라운드 연결을 시작함
                return ConnectionMultiplexer.Connect(options);
            });
            
            // [RedLock]: 분산 락 팩토리 및 제공자 통합 관리
            services.AddSingleton<IDistributedLockFactory>(sp => 
            {
                var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                return RedLockFactory.Create(new List<RedLockMultiplexer> { new RedLockMultiplexer(redis) });
            });
            services.AddSingleton<IRouletteLockProvider, MooldangBot.Infrastructure.Security.RouletteLockProvider>();

            // [v13.0] 파수꾼의 통합: 분산 상태 관리 및 오버레이 상태 등록
            services.AddSingleton<ILuaScriptProvider, MooldangBot.Infrastructure.Services.LuaScriptProvider>();
            services.AddSingleton<IOverlayState, MooldangBot.Infrastructure.State.OverlayState>();

            // [오시리스의 서판]: 채팅 로그 벌크 처리 서비스 등록
            services.AddSingleton<IChatLogBufferService, ChatLogBufferService>();

            // Database — [v10.11] AddPooledDbContextFactory 전환 (poolSize: 1024)
            // [오시리스의 영속]: 고부하 환경에서 컨텍스트 객체 자체를 풀링하여 재사용함으로써 메모리 할당 비용을 극한으로 낮춥니다.
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var serverVersion = ServerVersion.Parse("10.11-mariadb");
            
            services.AddPooledDbContextFactory<AppDbContext>(options =>
                options.UseMySql(connectionString, serverVersion, mysqlOptions =>
                {
                    mysqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    mysqlOptions.CommandTimeout(10);
                })
                .UseSnakeCaseNamingConvention(), poolSize: 1024);
            
            // 기존 Scoped 요청 및 concrete 클래스 주입을 위해 팩토리로부터 컨텍스트를 생성하는 브릿지 등록
            services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddScoped<ISongBookDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddScoped<IRouletteDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddScoped<IPointDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddScoped<ICommandDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // [v2.4.5] 치지직 게이트웨이(ChzzkAPI) 통신용 전용 클라이언트 구성
            services.AddHttpClient("ChzzkGateway", (sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var gatewayUrl = config["ChzzkApi:GatewayUrl"] ?? "http://chzzk-bot:8080";
                client.BaseAddress = new Uri(gatewayUrl);
                
                // 내부 API 보안 헤더 주입
                var secret = config["INTERNAL_API_SECRET"];
                if (!string.IsNullOrEmpty(secret))
                {
                    client.DefaultRequestHeaders.Add("X-Internal-Secret-Key", secret);
                }
            })
            .AddStandardResilienceHandler(); // 🔥 [오시리스의 방패]: 일시적 네트워크 오류 및 타임아웃 대응

            // [오시리스의 영혼]: 실제 치지직 API 클라이언트 등록 (게이트웨이 위임 방식)
            services.AddSingleton<IChzzkApiClient, ChzzkApiClient>();

            // [피닉스의 심장]: 실전 채팅 클라이언트 (게이트웨이 프록시 기반으로 전환 - 1단계)
            // [네임스페이스 명시]: Application.Interfaces의 규격을 구현한 프록시를 주입합니다.
            services.AddSingleton<MooldangBot.Contracts.Common.Interfaces.IChzzkChatClient, GatewayChatClientProxy>();
            // 향후 AI 답변 기능을 재활성화하려면 아래 줄의 주석을 해제하고 Mock 등록을 제거하십시오.
            // services.AddHttpClient<ILlmService, MooldangBot.Infrastructure.ApiClients.Philosophy.GeminiLlmService>();
            
            // AI 기능 호출 시 무응답(Silence) 처리를 위해 Mock 서비스를 등록합니다.
            services.AddSingleton<ILlmService, LlmServiceMock>();

            // [v2.4.5] ShardedWebSocketManager 등록 이관

            // [오시리스의 기록관]: 방송 통계 및 세션 관리
            services.AddSingleton<IBroadcastScribe, BroadcastScribe>();

            // [v1.2] 마스터 데이터 캐시 서비스 등록
            services.AddScoped<ICommandMasterCacheService, CommandMasterCacheService>();

            // [v7.0] Wallet Architecture: 포인트 캐시 등록 (워커는 WorkerRegistry에서 통합 관리)
            services.AddSingleton<IPointCacheService, PointCacheService>();

            // [v13.1] 리포지토리 등록
            // [Phase 2] ISongBookRepository는 SongBook 모듈에서 등록됩니다.

            // [v1.8] Safe Dynamic Query Engine 등록
            services.AddScoped<IDynamicQueryEngine, MooldangBot.Infrastructure.Services.Engines.DynamicQueryEngine>();

            // [v4.4.0] Dynamic Variable Resolver 등록
            services.AddScoped<IDynamicVariableResolver, MooldangBot.Infrastructure.Services.Engines.DynamicVariableResolver>();

            // [Migration]: Messaging infrastructure is now explicitly called in Program.cs of each host
            // services.AddMessagingInfrastructure(configuration);

            // [v4.0] COMMAND 엔드포인트 송신기는 이제 발신 전용(Fire & Forget)을 지향합니다.
            services.AddSingleton<IChzzkCommandSender, ChzzkCommandSender>();
            
            // [DEPRECATED v4.0] RPC 클라이언트는 차후 제거 예정입니다.
            services.AddSingleton<IChzzkRpcClient, ChzzkRpcClient>();

            // [v16.0] 지휘관 선호도 전용 기억 장치 (Redis Preference)
            services.AddSingleton<IPreferenceCacheService, PreferenceCacheService>();
            
            // [v16.1] 지휘관 영구 선호도 저장소 (MariaDB Preference)
            services.AddScoped<IPreferenceDbService, PreferenceDbService>();

            // [v13.0] 유튜브 실시간 정찰기(YouTube Recon Synergy) 등록
            services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();

            // [하모니의 창고]: 커스텀 아이콘 등을 위한 로컬 파일 저장소 등록
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

            // [v2.0] 영겁의 저장소: 분산 토큰 저장소 등록
            services.AddSingleton<IChzzkAccessCredentialStore, RedisTokenStore>();

            // [v4.0] 오시리스의 시동: 시스템 초기화 처리기 등록
            services.AddScoped<IDbInitializer, DbInitializer>();

            return services;
        }



        /// <summary>
        /// [오시리스의 전령]: MassTransit 기반의 고가용성 메시징 인프라를 설정합니다.
        /// </summary>
        public static IServiceCollection AddMessagingInfrastructure(
            this IServiceCollection services, 
            IConfiguration config, 
            params Assembly[] consumerAssemblies)
        {
            // [오시리스의 수리]: 다형성 메시징을 위한 글로벌 엔드포인트 컨벤션 설정.
            // 모든 ChzzkCommandBase 파생 명령들이 통합 큐(chzzk-commands-rpc)로 라우팅되도록 강제합니다.
            // [시니어 팁]: 구체적 타입을 명시하지 않으면 MassTransit은 발송 시 큐를 찾지 못할 수 있으므로, 리플렉션으로 모든 파생 타입을 등록합니다.
            var commandBaseType = typeof(MooldangBot.Contracts.Chzzk.Models.Commands.ChzzkCommandBase);
            var chzzkCommandTypes = commandBaseType.Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && commandBaseType.IsAssignableFrom(t));
            
            var rpcQueueUri = new Uri("queue:chzzk-commands-rpc");
            var mapMethod = typeof(EndpointConvention).GetMethod("Map", [typeof(Uri)]);
            
            foreach (var type in chzzkCommandTypes)
            {
                mapMethod?.MakeGenericMethod(type).Invoke(null, [rpcQueueUri]);
            }

            services.AddMassTransit(x =>
            {
                // [오시리스의 영명]: 모든 파생 명령 타입을 RequestClient로 등록하여 
                // ChzzkRpcClient에서 IServiceProvider를 통해 동적으로 꺼내 쓸 수 있게 합니다.
                foreach (var type in chzzkCommandTypes)
                {
                    x.AddRequestClient(type);
                }

                // 1. Consumer 자동 등록 (전달받은 어셈블리 기준)
                if (consumerAssemblies.Length > 0)
                {
                    x.AddConsumers(consumerAssemblies);
                }

                // 2. RabbitMQ 트랜스포트 설정
                x.UsingRabbitMq((context, cfg) =>
                {
                    // 환경 변수 연동
                    var host = config["RABBITMQ_HOST"] ?? "localhost";
                    var portStr = config["RABBITMQ_PORT"] ?? "5672";
                    var port = ushort.TryParse(portStr, out var p) ? p : (ushort)5672;
                    var virtualHost = config["RABBITMQ_VIRTUAL_HOST"] ?? "/";
                    var username = config["RABBITMQ_USER"] ?? "guest";
                    var password = config["RABBITMQ_PASS"] ?? "guest";

                    cfg.Host(host, port, virtualHost, h => 
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    // 🛡️ [오시리스의 방패 1] 글로벌 재시도 정책
                    cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(1)));

                    // 🛡️ [오시리스의 방패 2] 서킷 브레이커
                    cfg.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.ActiveThreshold = 10;
                        cb.TripThreshold = 15;
                        cb.ResetInterval = TimeSpan.FromMinutes(5);
                    });

                    // 🛡️ [오시리스의 수리] 3. Chzzk 전용 RPC 엔드포인트 설정
                    // 7개의 분리된 컨슈머를 하나의 'chzzk-commands-rpc' 큐로 통합하여 다형성 요청(Base -> Sub)이 정상 작동하도록 합니다.
                    // [시니어 팁]: 해당 Consumer들이 존재할 때만(즉, ChzzkAPI 프로젝트일 때만) 전용 큐를 생성하여 메시지 가로채기(Competition)를 방지합니다.
                    var chzzkCommandConsumers = consumerAssemblies
                        .SelectMany(assembly => assembly.GetTypes())
                        .Where(t => t.IsClass && !t.IsAbstract)
                        .Where(t => t.GetInterfaces().Any(i => 
                            i.IsGenericType && 
                            i.GetGenericTypeDefinition() == typeof(IConsumer<>) && 
                            typeof(MooldangBot.Contracts.Chzzk.Models.Commands.ChzzkCommandBase).IsAssignableFrom(i.GetGenericArguments()[0])))
                        .ToList();

                    if (chzzkCommandConsumers.Any())
                    {
                        cfg.ReceiveEndpoint("chzzk-commands-rpc", e => 
                        {
                            foreach (var consumerType in chzzkCommandConsumers)
                            {
                                e.ConfigureConsumer(context, consumerType);
                            }
                        });
                    }

                    // 🧠 [v6.0] 자율 복구 신경망: Saga 인프라 구성
                    cfg.ReceiveEndpoint("command-execution-saga", e => 
                    {
                        e.ConfigureSaga<Sagas.CommandExecutionSagaState>(context);
                    });

                    // ⚡ [P0 Quick Win] 10k TPS 대응: 동시 소비 한도 상향
                    // - ConcurrentMessageLimit: Consumer가 동시에 처리할 수 있는 메시지 수 (기본값 16 → 64)
                    // - PrefetchCount: RabbitMQ에서 Consumer로 미리 전달하는 메시지 수 (ConcurrentMessageLimit의 2배 권장)
                    cfg.PrefetchCount = 128;
                    cfg.ConcurrentMessageLimit = 64;

                    // 4. 엔드포인트 자동 구성 (이미 수동 구성된 컨슈머는 건너뜁니다)
                    cfg.ConfigureEndpoints(context);
                });

                // Saga State Machine 및 영속성 설정
                x.AddSagaStateMachine<Sagas.CommandExecutionSaga, Sagas.CommandExecutionSagaState>()
                    .EntityFrameworkRepository(r => 
                    {
                        r.ConcurrencyMode = ConcurrencyMode.Optimistic; // 낙관적 동시성 제어
                        r.ExistingDbContext<Persistence.AppDbContext>();
                        r.UseMySql(); // MariaDB 호환
                    });
            });

            return services;
        }
    }
}
