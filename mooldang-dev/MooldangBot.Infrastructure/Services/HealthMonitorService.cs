using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application.Services;
using StackExchange.Redis;
using MassTransit;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [심연의 눈 실구현체]: DB, Redis, RabbitMQ 그리고 내부 워커의 상태를 종합 점검합니다.
/// </summary>
public class HealthMonitorService(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    IBusControl busControl,
    ILogger<HealthMonitorService> logger)
{
    public async Task<HealthStatusReport> GetSystemPulseAsync(CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var report = new HealthStatusReport
        {
            CheckedAt = DateTime.UtcNow,
            MachineName = Environment.MachineName
        };

        // 1. [DB 점검]
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            report.Database = await dbContext.Database.CanConnectAsync(ct);
        }
        catch { report.Database = false; }

        // 2. [Redis/Messaging 점검]
        report.Redis = redis.IsConnected;
        
        var busHealth = busControl.CheckHealth();
        report.RabbitMQ = busHealth.Status == BusHealthStatus.Healthy;

        // 3. [함대 전체 맥박 수집]
        try
        {
            var allPulses = await db.HashGetAllAsync("pulse:v1:fleet");
            foreach (var entry in allPulses)
            {
                var json = entry.Value.ToString();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var instanceId = root.GetProperty("MachineName").GetString() ?? "Unknown";
                var workerName = root.GetProperty("WorkerName").GetString() ?? "Unknown";
                var lastPulse = root.GetProperty("LastPulseAt").GetDateTime();
                
                if (!report.FleetInstances.TryGetValue(instanceId, out var instance))
                {
                    instance = new FleetInstanceReport { MachineName = instanceId };
                    report.FleetInstances[instanceId] = instance;
                }

                bool isAlive = (DateTime.UtcNow - lastPulse).TotalMinutes < 1;
                instance.Workers[workerName] = isAlive;
                
                instance.MemoryUsageMb = root.GetProperty("MemoryUsageMb").GetInt64();
                instance.CpuTimeMs = root.GetProperty("CpuTimeMs").GetDouble();
                instance.LastSeenAt = lastPulse > instance.LastSeenAt ? lastPulse : instance.LastSeenAt;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"[HealthMonitor] 함대 정보 수집 실패: {ex.Message}");
        }

        return report;
    }
}
