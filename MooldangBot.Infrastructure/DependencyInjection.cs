using Polly;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Infrastructure.ApiClients;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Infrastructure.Messaging;
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
using MooldangBot.Infrastructure.Security;

namespace MooldangBot.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // [이지스 파이프라인]: 표준 분산 캐시 인터페이스 등록 (현재는 메모리 기반)
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddScoped<IIdentityCacheService, IdentityCacheService>();
            services.AddSingleton<INotificationService, NotificationService>();

            // [v2.4.6] 오시리스의 세션: 봇 엔진 등 백그라운드 환경용 기본 세션 등록
            // API 환경에서는 Presentation 레이어에서 등록된 실제 UserSession으로 덮어씌워집니다.
            services.TryAddScoped<IUserSession, BotUserSession>();

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

            // Database — [Phase4] AddDbContextPool 상향 (poolSize: 256)
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            // [파로스]: 실 서비스 기동 시에도 DB 응답 대기 없이 즉시 설정을 완료하도록 버전을 고정합니다.
            var serverVersion = ServerVersion.Parse("10.11-mariadb");
            
            services.AddDbContextPool<AppDbContext>(options =>
                options.UseMySql(connectionString, serverVersion, mysqlOptions =>
                {
                    mysqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    mysqlOptions.CommandTimeout(10);
                })
                .UseSnakeCaseNamingConvention(), poolSize: 256);
            
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // [v2.4.5] IChzzkApiClient 및 IChzzkChatClient 등록은 각 호스트(Api/ChzzkAPI) 레벨로 이관되었습니다.
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

            // [v1.9.9] 전문 로그 모니터링을 위한 RabbitMQ 인프라 구성
            services.AddSingleton<IConnectionFactory>(sp => 
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var host = configuration["RABBITMQ_HOST"]!;
                var port = int.TryParse(configuration["RABBITMQ_PORT"], out var p) ? p : 5672;
                var user = configuration["RABBITMQ_USER"]!;
                var pass = configuration["RABBITMQ_PASS"]!;

                return new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = user,
                    Password = pass,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    RequestedHeartbeat = TimeSpan.FromSeconds(10)
                };
            });

            services.AddSingleton<RabbitMQPersistentConnection>();
            services.AddSingleton<IRabbitMqService, RabbitMqService>();

            // [v16.0] 지휘관 선호도 전용 기억 장치 (Redis Preference)
            services.AddSingleton<IPreferenceCacheService, PreferenceCacheService>();
            
            // [v16.1] 지휘관 영구 선호도 저장소 (MariaDB Preference)
            services.AddScoped<IPreferenceDbService, PreferenceDbService>();

            // [v13.0] 유튜브 실시간 정찰기(YouTube Recon Synergy) 등록
            services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();

            // [하모니의 창고]: 커스텀 아이콘 등을 위한 로컬 파일 저장소 등록
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

            // [v2.0] 영겁의 저장소: 분산 토큰 저장소 및 멱등성 가드 등록
            services.AddSingleton<IChzzkTokenStore, RedisTokenStore>();
            services.AddSingleton<IIdempotencyService, IdempotencyService>();

            // [v4.0] 오시리스의 시동: 시스템 초기화 처리기 등록
            services.AddScoped<IDbInitializer, DbInitializer>();

            return services;
        }

        public static IServiceCollection AddRabbitMqConsumer(this IServiceCollection services)
        {
            services.AddHostedService<RabbitMqConsumerService>();
            return services;
        }
    }
}
