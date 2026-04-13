using Polly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Infrastructure.ApiClients;
using MooldangBot.Infrastructure.Persistence;
// [Migration]: Legacy Messaging namespace removed in favor of MassTransit
using MooldangBot.Infrastructure.ApiClients.Philosophy;
using MooldangBot.Application.Services.Philosophy;
using MooldangBot.Infrastructure.Services;
using MooldangBot.Application.Workers;
using StackExchange.Redis;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using RabbitMQ.Client;
using MooldangBot.Infrastructure.Services.Background;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MooldangBot.Infrastructure.Security;
using MassTransit;
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

            // [v2.4.7] 수호자의 방패: 데이터 보호 서비스 등록 (열쇠 전역 공유)
            // 봇 엔진(ChzzkAPI)에서도 API가 저장한 암호화 토큰을 읽을 수 있도록 DB에 키를 저장합니다.
            services.AddDataProtection()
                .SetApplicationName("MooldangBot")
                .PersistKeysToDbContext<AppDbContext>();
            
            // [v13.1] 파로스의 등대: Snowflake 전역 ID 생성기 등록 (Singleton)
            services.AddSingleton<ISongLibraryIdGenerator, SnowflakeIdGenerator>();
            // [v13.1] 스테이징 데이터 수명 주기 관리 워커 등록
            services.AddHostedService<StagingCleanupWorker>();
            
            // [Phase 9] 심연의 맥박: 건강 모니터링 및 알림용 서비스
            services.AddSingleton<IPulseService, PulseService>();
            services.AddHttpClient();
            services.AddSingleton<IHealthMonitorService, HealthMonitorService>();
            // [Phase 18.5] 심연의 지배자 (Chaos) - 함대 전역 상태 일관성을 위해 반드시 Singleton
            services.AddSingleton<IChaosManager, ChaosManager>();

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

            // [v13.0] 파로스의 등대: 분산 상태 관리 서비스 등록
            services.AddSingleton<ILuaScriptProvider, LuaScriptProvider>();
            services.AddSingleton<MooldangBot.Application.State.RouletteState>();
            services.AddSingleton<MooldangBot.Application.State.OverlayState>();
            services.AddSingleton<MooldangBot.Application.Features.SongBook.SongBookState>();

            // [오시리스의 서판]: 채팅 로그 벌크 처리 서비스 등록
            services.AddSingleton<IChatLogBufferService, ChatLogBufferService>();
            services.AddHostedService<ChatLogBatchWorker>();

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
            services.AddSingleton<MooldangBot.Application.Interfaces.IChzzkChatClient, GatewayChatClientProxy>();
            // 향후 AI 답변 기능을 재활성화하려면 아래 줄의 주석을 해제하고 Mock 등록을 제거하십시오.
            // services.AddHttpClient<ILlmService, MooldangBot.Infrastructure.ApiClients.Philosophy.GeminiLlmService>();
            
            // AI 기능 호출 시 무응답(Silence) 처리를 위해 Mock 서비스를 등록합니다.
            services.AddSingleton<ILlmService, LlmServiceMock>();

            // [v2.4.5] ShardedWebSocketManager 등록 이관

            // [오시리스의 기록관]: 방송 통계 및 세션 관리
            services.AddSingleton<IBroadcastScribe, BroadcastScribe>();

            // [v1.2] 마스터 데이터 캐시 서비스 등록
            services.AddScoped<ICommandMasterCacheService, CommandMasterCacheService>();

            // [v13.1] 리포지토리 등록
            services.AddScoped<ISongBookRepository, MooldangBot.Infrastructure.Persistence.Repositories.SongBookRepository>();

            // [v1.8] Safe Dynamic Query Engine 등록
            services.AddScoped<IDynamicQueryEngine, MooldangBot.Infrastructure.Services.Engines.DynamicQueryEngine>();

            // [v4.4.0] Dynamic Variable Resolver 등록
            services.AddScoped<IDynamicVariableResolver, MooldangBot.Infrastructure.Services.Engines.DynamicVariableResolver>();

            // [Migration]: Messaging infrastructure is now explicitly called in Program.cs of each host
            // services.AddMessagingInfrastructure(configuration);

            // [v4.0] RPC 클라이언트는 이제 MassTransit IRequestClient를 사용합니다.
            services.AddSingleton<IChzzkRpcClient, ChzzkRpcClient>();

            // [v16.0] 지휘관 선호도 전용 기억 장치 (Redis Preference)
            services.AddSingleton<IPreferenceCacheService, PreferenceCacheService>();
            
            // [v16.1] 지휘관 영구 선호도 저장소 (MariaDB Preference)
            services.AddScoped<IPreferenceDbService, PreferenceDbService>();

            // [v13.0] 유튜브 실시간 정찰기(YouTube Recon Synergy) 등록
            services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();

            // [하모니의 창고]: 커스텀 아이콘 등을 위한 로컬 파일 저장소 등록
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

            // [v2.0] 영겁의 저장소: 분산 토큰 저장소 및 멱등성 가드 등록
            services.AddSingleton<IChzzkAccessCredentialStore, RedisTokenStore>();
            services.AddSingleton<IIdempotencyService, IdempotencyService>();

            // [v4.0] 오시리스의 시동: 시스템 초기화 처리기 등록
            services.AddScoped<IDbInitializer, DbInitializer>();

            return services;
        }

        /// <summary>
        /// [v3.7] 치지직 이벤트 소비자 등록 (메인 API 전용)
        /// RabbitMQ에서 이벤트를 수신하여 MediatR로 전파합니다.
        /// </summary>
        public static IServiceCollection AddChzzkEventConsumer(this IServiceCollection services)
        {
            // [DEPRECATED]: MassTransit 도입으로 인해 ChzzkEventRabbitMqConsumer는 더 이상 필요하지 않습니다.
            // 서비스 안정화 기간 동안 주석 처리 후 제거 예정입니다.
            // services.AddHostedService<ChzzkEventRabbitMqConsumer>();
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
            EndpointConvention.Map<MooldangBot.Contracts.Integrations.Chzzk.Models.Commands.ChzzkCommandBase>(new Uri("queue:chzzk-commands-rpc"));

            services.AddMassTransit(x =>
            {
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
                    cfg.ReceiveEndpoint("chzzk-commands-rpc", e => 
                    {
                        // 전달된 어셈블리 내 모든 Consumer 중 ChzzkCommandBase 관련 소비자들만 필터링하여 이 큐에 할당
                        foreach (var assembly in consumerAssemblies)
                        {
                            var chzzkConsumers = assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>) 
                                              && typeof(MooldangBot.Contracts.Integrations.Chzzk.Models.Commands.ChzzkCommandBase).IsAssignableFrom(i.GetGenericArguments()[0])));

                            foreach (var consumerType in chzzkConsumers)
                            {
                                e.ConfigureConsumer(context, consumerType);
                            }
                        }
                    });

                    // 4. 엔드포인트 자동 구성 (이미 수동 구성된 컨슈머는 건너뜁니다)
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
