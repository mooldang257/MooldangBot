using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Foundation.Persistence;
using StackExchange.Redis;
using MassTransit;
using System.Reflection;
using MooldangBot.Foundation.ApiClients;
using MooldangBot.Foundation.Services;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

namespace MooldangBot.Foundation;

public static class DependencyInjection
{
    /// <summary>
    /// [파운데이션]: 모든 서비스가 공통으로 사용하는 최소한의 기술적 기반을 주입합니다.
    /// </summary>
    public static IServiceCollection AddFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Redis 설정 (N3/M3: 연결 블로킹 방지)
        var redisUrl = configuration["REDIS_URL"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp => 
        {
            var options = ConfigurationOptions.Parse(redisUrl);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 5;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<RedLockNet.IDistributedLockFactory>(sp => 
        {
            var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            var multiplexers = new List<RedLockMultiplexer> { new RedLockMultiplexer(multiplexer) };
            return RedLockFactory.Create(multiplexers);
        });

        // 2. Core DbContext 설정
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var serverVersion = new MariaDbServerVersion(new Version(11, 8, 0));

        services.AddDbContext<CoreDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(3);
                mysqlOptions.CommandTimeout(10);
            }));

        // 3. 기초 기술 서비스 등록
        services.AddSingleton<PulseService>();
        services.AddScoped<HealthMonitorService>();
        services.AddSingleton<INotificationService, NotificationService>();
        
        // Naver API Client (Foundation 버전)
        services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>();

        return services;
    }

    /// <summary>
    /// [파운데이션]: 핵심 워커들을 등록합니다. (게이트웨이/봇 공용)
    /// </summary>
    public static IServiceCollection AddFoundationWorkers(this IServiceCollection services)
    {
        services.AddHostedService<Workers.SystemWatchdogService>();
        services.AddHostedService<Workers.ChzzkBackgroundService>();
        return services;
    }

    /// <summary>
    /// [파운데이션]: MassTransit 메시징 기초 인프라를 주입합니다.
    /// </summary>
    public static IServiceCollection AddFoundationMessaging(this IServiceCollection services, IConfiguration configuration, params Assembly[] consumerAssemblies)
    {
        services.AddMassTransit(x =>
        {
            if (consumerAssemblies != null && consumerAssemblies.Length > 0)
            {
                x.AddConsumers(consumerAssemblies);
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RABBITMQ_HOST"] ?? "localhost";
                var port = configuration["RABBITMQ_PORT"] ?? "5672";
                var user = configuration["RABBITMQ_USER"] ?? "guest";
                var pass = configuration["RABBITMQ_PASS"] ?? "guest";

                cfg.Host(host, ushort.Parse(port), "/", h =>
                {
                    h.Username(user);
                    h.Password(pass);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
