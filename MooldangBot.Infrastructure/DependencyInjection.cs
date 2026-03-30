using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Infrastructure.ApiClients;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Infrastructure.ApiClients.Philosophy;
using MooldangBot.Infrastructure.ApiClients.Philosophy.Sharding;
using MooldangBot.Application.Services.Philosophy;
using MooldangBot.Infrastructure.Services;
using MooldangBot.Application.Workers;
using StackExchange.Redis;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MooldangBot.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // [오시리스의 영속]: Redis 인프라 구성 (N3/M3: 동기 블로킹 방지 및 지연 연결 보장)
            var redisUrl = configuration["REDIS_URL"] ?? "localhost:6379";
            
            services.AddSingleton<IConnectionMultiplexer>(sp => 
            {
                var options = ConfigurationOptions.Parse(redisUrl);
                options.AbortOnConnectFail = false; // [핵심] 연결 실패 시에도 즉시 객체를 반환하여 앱 기동 블로킹 방지
                options.ConnectRetry = 5;
                options.ConnectTimeout = 5000;      
                
                // [시니어 팁]: 기동 시점에 동기로 기다리지 않고 백그라운드 연결을 시작함
                return ConnectionMultiplexer.Connect(options);
            });
            
            // [RedLock]: 분산 락 팩토리 통합 관리
            services.AddSingleton<IDistributedLockFactory>(sp => 
            {
                var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                return RedLockFactory.Create(new List<RedLockMultiplexer> { new RedLockMultiplexer(redis) });
            });

            // Database — [Phase4] AddDbContextPool 상향 (poolSize: 256)
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            // [파로스]: 실 서비스 기동 시에도 DB 응답 대기 없이 즉시 설정을 완료하도록 버전을 고정합니다.
            var serverVersion = ServerVersion.Parse("10.11-mariadb");
            
            services.AddDbContextPool<AppDbContext>(options =>
                options.UseMySql(connectionString, serverVersion, mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    mysqlOptions.CommandTimeout(10);
                }), poolSize: 256);
            
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Api Clients — [12-2]: Polly 표준 탄력성 핸들러 적용 (Retry, CircuitBreaker, Timeout 등)
            services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>()
                .AddStandardResilienceHandler();
            
            // [거울의 신경망]: Gemini API 실전 연동
            services.AddHttpClient<ILlmService, MooldangBot.Infrastructure.ApiClients.Philosophy.GeminiLlmService>();

            // [피닉스의 심장]: 실전 채팅 클라이언트 (샤드 분할 관리형)
            services.AddSingleton<IChzzkChatClient, ShardedWebSocketManager>();

            // [오시리스의 기록관]: 방송 통계 및 세션 관리
            services.AddSingleton<IBroadcastScribe, BroadcastScribe>();

            // [v1.2] 마스터 데이터 캐시 서비스 등록
            services.AddScoped<ICommandMasterCacheService, CommandMasterCacheService>();

            // [v1.8] Safe Dynamic Query Engine 등록
            services.AddScoped<IDynamicQueryEngine, MooldangBot.Infrastructure.Services.Engines.DynamicQueryEngine>();

            // [v4.4.0] Dynamic Variable Resolver 등록
            services.AddScoped<IDynamicVariableResolver, MooldangBot.Infrastructure.Services.Engines.DynamicVariableResolver>();
            
            // [v4.5.1] RabbitMQ 비동기 전령 서비스 등록
            services.AddSingleton<IRabbitMqService, RabbitMqService>();

            // [v4.5.1] RabbitMQ POC 소비자 워커 등록
            services.AddHostedService<RabbitMqConsumerService>();

            // [v4.3.0] 분산 헬스 체크 시스템 등록
            services.AddHealthChecks()
                .AddCheck<BotHealthCheck>("MooldangBot_Shards");

            return services;
        }
    }
}
