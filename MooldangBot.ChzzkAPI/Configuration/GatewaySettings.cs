namespace MooldangBot.ChzzkAPI.Configuration;

public class GatewaySettings
{
    public int InitialShardCount { get; init; } = 1;
    public int MaxShards { get; init; } = 10;
    public int ReconnectDelaySeconds { get; init; } = 5;
}