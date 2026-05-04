using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities.Philosophy;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [오시리스의 항아리]: 실전 로그 데이터를 안전하게 수집하는 스레드 안전 버퍼 구현체입니다.
/// </summary>
public class LogBulkBuffer
{
    private ConcurrentQueue<LogIamfVibrations> _vibrationLogs = new();
    private ConcurrentQueue<IamfScenarios> _scenarios = new();
    
    // [오시리스의 저울]: 최대 버퍼 크기 제한 (메모리 보호)
    private const int MaxBufferSize = 100_000;
    private readonly ILogger<LogBulkBuffer> _logger;

    public LogBulkBuffer(ILogger<LogBulkBuffer> logger)
    {
        _logger = logger;
    }

    public void AddVibrationLog(LogIamfVibrations log)
    {
        if (_vibrationLogs.Count > MaxBufferSize)
        {
            _logger.LogWarning("⚠️ [LogBulkBuffer] 진동 로그 버퍼 초과. 로그를 드롭합니다.");
            return;
        }
        _vibrationLogs.Enqueue(log);
    }

    public void AddScenario(IamfScenarios scenario)
    {
        if (_scenarios.Count > MaxBufferSize)
        {
            _logger.LogWarning("⚠️ [LogBulkBuffer] 시나리오 로그 버퍼 초과. 로그를 드롭합니다.");
            return;
        }
        _scenarios.Enqueue(scenario);
    }

    public List<LogIamfVibrations> DrainVibrationLogs()
    {
        // [원자적 교체]: 데이터 유실 방지
        var oldQueue = Interlocked.Exchange(ref _vibrationLogs, new ConcurrentQueue<LogIamfVibrations>());
        return oldQueue.ToList();
    }

    public List<IamfScenarios> DrainScenarios()
    {
        var oldQueue = Interlocked.Exchange(ref _scenarios, new ConcurrentQueue<IamfScenarios>());
        return oldQueue.ToList();
    }
}
