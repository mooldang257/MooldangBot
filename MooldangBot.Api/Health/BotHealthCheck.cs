using Microsoft.Extensions.Diagnostics.HealthChecks;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Chzzk.Models;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;

namespace MooldangBot.Api.Health;

/// <summary>
/// [오시리스의 감시]: WebSocketShard 메트릭 및 인프라(DB, Redis, RabbitMQ) 상태를 포함한 통합 헬스 체크입니다.
/// (Refined): Scoped Dependency 해결을 위해 IServiceScopeFactory 사용 버전
/// </summary>
public class BotHealthCheck(
    IChzzkChatClient chatClient, 
    IConnectionMultiplexer redis,
    IBusControl busControl, // 🔥 MassTransit 버스 컨트롤 주입
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var shardStatuses = (await chatClient.GetShardStatusesAsync()).ToList();
        var totalConnections = shardStatuses.Sum(s => s.ConnectionCount);
        var unhealthyShards = shardStatuses.Count(s => !s.IsHealthy);
        
        var isRedisConnected = redis.IsConnected;
        
        // [오시리스의 전령]: MassTransit 버스 건강 상태 확인
        var busHealth = busControl.CheckHealth();
        var isRabbitMqConnected = busHealth.Status == BusHealthStatus.Healthy;
        
        // 🛡️ [오시리스의 방패]: Scoped DbContext를 안전하게 조회하기 위해 직접 Scope를 생성합니다.
        bool isDbConnected;
        try 
        { 
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            isDbConnected = await db.Database.CanConnectAsync(cancellationToken); 
        }
        catch { isDbConnected = false; }

        var data = new Dictionary<string, object>
        {
            { "TotalConnections", totalConnections },
            { "InstanceShardCount", shardStatuses.Count },
            { "UnhealthyShards", unhealthyShards },
            { "DbConnected", isDbConnected },
            { "RedisConnected", isRedisConnected },
            { "RabbitMqConnected", isRabbitMqConnected },
            { "Shards", shardStatuses }
        };

        if (unhealthyShards > 0 || !isRedisConnected || !isRabbitMqConnected || !isDbConnected)
        {
            return HealthCheckResult.Degraded("[오시리스의 경고] 일부 인프라 또는 샤드가 비정상 상태입니다.", data: data);
        }

        return HealthCheckResult.Healthy("[오시리스의 안녕] 모든 인프라 및 샤드가 정상 가동 중입니다.", data: data);
    }
}
