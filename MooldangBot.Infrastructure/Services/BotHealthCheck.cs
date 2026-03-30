using Microsoft.Extensions.Diagnostics.HealthChecks;
using MooldangBot.Application.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 감시]: WebSocketShard 메트릭 및 인프라(Redis, RabbitMQ) 상태를 포함한 통합 헬스 체크입니다.
/// </summary>
public class BotHealthCheck : IHealthCheck
{
    private readonly IChzzkChatClient _chatClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly IRabbitMqService _rabbitMq;

    public BotHealthCheck(
        IChzzkChatClient chatClient, 
        IConnectionMultiplexer redis,
        IRabbitMqService rabbitMq)
    {
        _chatClient = chatClient;
        _redis = redis;
        _rabbitMq = rabbitMq;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var shardStatuses = _chatClient.GetShardStatuses().ToList();
        var totalConnections = shardStatuses.Sum(s => s.ConnectionCount);
        var unhealthyShards = shardStatuses.Count(s => !s.IsHealthy);
        
        var isRedisConnected = _redis.IsConnected;
        var isRabbitMqConnected = _rabbitMq.IsConnected;

        var data = new Dictionary<string, object>
        {
            { "TotalConnections", totalConnections },
            { "InstanceShardCount", shardStatuses.Count },
            { "UnhealthyShards", unhealthyShards },
            { "RedisConnected", isRedisConnected },
            { "RabbitMqConnected", isRabbitMqConnected },
            { "Shards", shardStatuses }
        };

        if (unhealthyShards > 0 || !isRedisConnected || !isRabbitMqConnected)
        {
            return Task.FromResult(HealthCheckResult.Degraded("[오시리스의 경고] 일부 인프라 또는 샤드가 비정상 상태입니다.", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("[오시리스의 안녕] 모든 인프라 및 샤드가 정상 가동 중입니다.", data: data));
    }
}
