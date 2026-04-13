namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [맥박의 복구]: 모든 백그라운드 워커의 생존 신호를 관리하는 서비스입니다.
/// </summary>
public interface IPulseService
{
    /// <summary>
    /// 특정 워커의 생존 신호를 기록합니다.
    /// </summary>
    void ReportPulse(string workerName);

    /// <summary>
    /// 마지막 맥박 시간을 조회합니다.
    /// </summary>
    DateTime? GetLastPulseAt(string workerName);

    /// <summary>
    /// 현재 모든 워커의 맥박 상태를 조회합니다.
    /// </summary>
    Dictionary<string, DateTime> GetAllPulses();
}

/// <summary>
/// [심연의 눈]: 시스템의 전체적인 건강 상태를 한눈에 파악할 수 있게 해주는 서비스입니다.
/// </summary>
public interface IHealthMonitorService
{
    /// <summary>
    /// 전체 전공 인프라 및 워커의 건강 상태를 수집합니다.
    /// </summary>
    Task<HealthStatusReport> GetSystemPulseAsync(CancellationToken ct = default);
}

public class HealthStatusReport
{
    public bool Database { get; set; }
    public bool Redis { get; set; }
    public bool RabbitMQ { get; set; }
    public string MachineName { get; set; } = "";
    public Dictionary<string, FleetInstanceReport> FleetInstances { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class FleetInstanceReport
{
    public string MachineName { get; set; } = "";
    public long MemoryUsageMb { get; set; }
    public double CpuTimeMs { get; set; }
    public Dictionary<string, bool> Workers { get; set; } = new();
    public DateTime LastSeenAt { get; set; }
}
