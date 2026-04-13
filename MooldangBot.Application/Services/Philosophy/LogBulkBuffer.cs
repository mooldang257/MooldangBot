using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities.Philosophy;
using System.Collections.Concurrent;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [오시리스의 항아리]: 실전 로그 데이터를 안전하게 수집하는 스레드 안전 버퍼 구현체입니다.
/// </summary>
public class LogBulkBuffer : ILogBulkBuffer
{
    private ConcurrentBag<IamfVibrationLog> _vibrationLogs = new();
    private ConcurrentBag<IamfScenario> _scenarios = new();

    public void AddVibrationLog(IamfVibrationLog log) => _vibrationLogs.Add(log);
    public void AddScenario(IamfScenario scenario) => _scenarios.Add(scenario);

    public List<IamfVibrationLog> DrainVibrationLogs()
    {
        var logs = _vibrationLogs.ToList();
        _vibrationLogs = new ConcurrentBag<IamfVibrationLog>();
        return logs;
    }

    public List<IamfScenario> DrainScenarios()
    {
        var scenarios = _scenarios.ToList();
        _scenarios = new ConcurrentBag<IamfScenario>();
        return scenarios;
    }
}
