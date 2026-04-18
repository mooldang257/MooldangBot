using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Contracts.Chzzk;
using System.Text.Json;

namespace MooldangBot.Application.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddMooldangSignalR(this IServiceCollection services, IConfiguration configuration)
    {
        var redisUrl = configuration["REDIS_URL"]!;
        
        services.AddSignalR(options => {
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(20);
        })
        .AddStackExchangeRedis(redisUrl, options => {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("MooldangBot");
        })
        .AddJsonProtocol(options => {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

        services.AddStackExchangeRedisCache(options => {
            options.Configuration = redisUrl;
            options.InstanceName = "MooldangBot_";
        });

        return services;
    }
}
