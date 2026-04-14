namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [심연의 시련]: 개발/스테이징 환경에서 무작위 장애를 주입하거나 Redis 상태를 시뮬레이션하는 인터페이스입니다.
/// </summary>
public interface IChaosManager
{
    bool IsChaosEnabled { get; set; }
    bool IsRedisPanic { get; }
    Task TryInjectFaultAsync(string resource);
    void TriggerRedisPanic(TimeSpan? duration = null);
    void TriggerApiDelay(TimeSpan? duration = null);
    void Reset();
}
