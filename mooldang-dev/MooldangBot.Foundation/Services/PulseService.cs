using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using MooldangBot.Domain.Common;

namespace MooldangBot.Foundation.Services;

/// <summary>
/// [파운데이션]: 함대 인스턴스와 워커들의 생존 신호(Pulse)를 Redis에 기록합니다.
/// </summary>
public class PulseService(IConnectionMultiplexer redis, ILogger<PulseService> logger)
{
    private readonly IDatabase _db = redis.GetDatabase();

    public void ReportPulse(string workerName)
    {
        try
        {
            var pulse = new
            {
                MachineName = Environment.MachineName,
                WorkerName = workerName,
                LastPulseAt = DateTime.UtcNow,
                MemoryUsageMb = GC.GetTotalMemory(false) / 1024 / 1024,
                CpuTimeMs = 0 // 필요 시 구현
            };

            var json = JsonSerializer.Serialize(pulse);
            _db.HashSet("pulse:v1:fleet", $"{Environment.MachineName}:{workerName}", json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Pulse] 맥박 보고 중 오류: {Msg}", ex.Message);
        }
    }
}
