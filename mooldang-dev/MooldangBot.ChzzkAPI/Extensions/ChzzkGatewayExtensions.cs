using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Contracts.Chzzk;
using System.Text.Json;

namespace MooldangBot.ChzzkAPI.Extensions;

public static class ChzzkGatewayExtensions
{
    /// <summary>
    /// 게이트웨이 전용 SignalR 설정 (Redis Backplane 포함)
    /// </summary>
    public static IServiceCollection AddGatewaySignalR(this IServiceCollection services, IConfiguration configuration)
    {
        var redisUrl = configuration["REDIS_URL"] ?? "localhost:6379";
        
        services.AddSignalR(options => {
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(20);
        })
        .AddStackExchangeRedis(redisUrl, options => {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("MooldangBot");
        })
        .AddJsonProtocol(options => {
            options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

        return services;
    }

    /// <summary>
    /// 게이트웨이 전용 최소 유효성 검사 설정
    /// </summary>
    public static IServiceCollection AddGatewayVersioning(this IServiceCollection services)
    {
        // 게이트웨이는 비즈니스 검증이 거의 없으므로 기본 API 기능만 활성화
        return services;
    }
}
