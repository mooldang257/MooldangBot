using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using StackExchange.Redis;
using MassTransit;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [심연의 눈 실구현체]: DB, Redis, RabbitMQ 그리고 내부 워커의 상태를 종합 점검합니다.
/// [v4.0] RabbitMQ 연결 확인은 MassTransit 버스 컨트롤 상태를 통해 수행됩니다.
/// </summary>
public class HealthMonitorService(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    IBusControl busControl, // 🔥 MassTransit 버스 컨트롤 주입
    IPulseService pulseService,
    ILogger<HealthMonitorService> logger) : IHealthMonitorService
{
    public async Task<HealthStatusReport> GetSystemPulseAsync(CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var report = new HealthStatusReport
        {
            CheckedAt = DateTime.UtcNow,
            MachineName = Environment.MachineName
        };

        // 1. [DB 점검]: MariaDB 연결성 확인
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            report.Database = await dbContext.Database.CanConnectAsync(ct);
        }
        catch { report.Database = false; }

        // 2. [Redis/Messaging 점검]
        report.Redis = redis.IsConnected;
        
        // [오시리스의 전령]: MassTransit 건강 상태 확인
        var busHealth = busControl.CheckHealth();
        report.RabbitMQ = busHealth.Status == BusHealthStatus.Healthy;

        // 3. [함대 전체 맥박 수집]: Redis Hash에서 모든 인스턴스의 정보 취합
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
                
                // 인스턴스 정보가 없으면 초기화
                if (!report.FleetInstances.TryGetValue(instanceId, out var instance))
                {
                    instance = new FleetInstanceReport { MachineName = instanceId };
                    report.FleetInstances[instanceId] = instance;
                }

                // 워커 상태 기록 (최근 1분 이내면 정상)
                bool isAlive = (DateTime.UtcNow - lastPulse).TotalMinutes < 1;
                instance.Workers[workerName] = isAlive;
                
                // 리소스 지표 기록 (가장 최근 보고 기준)
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
