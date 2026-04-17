namespace MooldangBot.Contracts.Common.Models;

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
