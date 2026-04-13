using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace MooldangBot.Application.Services;

/// <summary>
/// [맥박의 집결지]: 모든 워커의 생존 신호와 시스템 부하 정보를 Redis에서 통합 관리합니다.
/// [v14.0] 다중 인스턴스 환경에서 어떤 컨테이너가 아픈지 전역적으로 감시할 수 있습니다.
/// </summary>
public class PulseService(IConnectionMultiplexer redis, ILogger<PulseService> logger) : IPulseService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string HashKey = "pulse:v1:fleet";
    private readonly string _machineName = Environment.MachineName;

    public void ReportPulse(string workerName)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var info = new
            {
                LastPulseAt = DateTime.UtcNow,
                MemoryUsageMb = process.WorkingSet64 / 1024 / 1024,
                CpuTimeMs = process.TotalProcessorTime.TotalMilliseconds,
                MachineName = _machineName,
                WorkerName = workerName
            };

            string fullKey = $"{_machineName}:{workerName}";
            string json = JsonSerializer.Serialize(info);

            // Redis Hash에 기록 (Fleet Dashboard에서 한꺼번에 HGETALL로 조회 가능)
            _db.HashSet(HashKey, fullKey, json);
            
            // [이지스]: 해당 필드에 대해 별도의 만료 처리가 불가능하므로, 전체 맵의 수명은 HSET 시점마다 갱신
            _db.KeyExpire(HashKey, TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            logger.LogWarning($"[PulseService] 맥박 보고 실패: {ex.Message}");
        }
    }

    public DateTime? GetLastPulseAt(string workerName)
    {
        string fullKey = $"{_machineName}:{workerName}";
        var val = _db.HashGet(HashKey, fullKey);
        if (val.HasValue)
        {
            using var doc = JsonDocument.Parse(val.ToString()!);
            if (doc.RootElement.TryGetProperty("LastPulseAt", out var prop))
            {
                return prop.GetDateTime();
            }
        }
        return null;
    }

    public Dictionary<string, DateTime> GetAllPulses()
    {
        var all = _db.HashGetAll(HashKey);
        var result = new Dictionary<string, DateTime>();

        foreach (var entry in all)
        {
            string key = entry.Name!;
            if (key.StartsWith(_machineName + ":"))
            {
                using var doc = JsonDocument.Parse(entry.Value.ToString()!);
                if (doc.RootElement.TryGetProperty("LastPulseAt", out var prop))
                {
                    result[key.Split(':')[1]] = prop.GetDateTime();
                }
            }
        }
        return result;
    }
}
