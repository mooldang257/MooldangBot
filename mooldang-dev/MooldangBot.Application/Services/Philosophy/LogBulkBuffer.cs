using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities.Philosophy;
using System.Collections.Concurrent;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [오시리스의 항아리]: 실전 로그 데이터를 안전하게 수집하는 스레드 안전 버퍼 구현체입니다.
/// </summary>
public class LogBulkBuffer
{
    private ConcurrentBag<LogIamfVibrations> _vibrationLogs = new();
    private ConcurrentBag<IamfScenarios> _scenarios = new();

    public void AddVibrationLog(LogIamfVibrations log) => _vibrationLogs.Add(log);
    public void AddScenario(IamfScenarios scenario) => _scenarios.Add(scenario);

    public List<LogIamfVibrations> DrainVibrationLogs()
    {
        var logs = _vibrationLogs.ToList();
        _vibrationLogs = new ConcurrentBag<LogIamfVibrations>();
        return logs;
    }

    public List<IamfScenarios> DrainScenarios()
    {
        var scenarios = _scenarios.ToList();
        _scenarios = new ConcurrentBag<IamfScenarios>();
        return scenarios;
    }
}
