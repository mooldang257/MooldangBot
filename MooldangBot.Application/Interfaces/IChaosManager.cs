using System;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [심연의 지배자]: 함선의 가상 장애 상태(Chaos Mode)를 제어하고 조회하는 인터페이스입니다.
/// </summary>
public interface IChaosManager
{
    /// <summary>카오스 모드 활성화 여부</summary>
    bool IsChaosEnabled { get; set; }

    /// <summary>가상 Redis 장애 활성화 여부</summary>
    bool IsRedisPanic { get; }

    /// <summary>가상 API 지연 활성화 여부</summary>
    bool IsApiDelayed { get; }

    /// <summary>특정 기능에 인위적인 장애를 주입합니다. (비동기)</summary>
    Task TryInjectFaultAsync(string featureName);

    /// <summary>카오스 모드 활성화 (5분간 지속)</summary>
    void TriggerRedisPanic(TimeSpan? duration = null);

    /// <summary>가상 API 지연 트리거</summary>
    void TriggerApiDelay(TimeSpan? duration = null);

    /// <summary>모든 시련 중단</summary>
    void Reset();
}
