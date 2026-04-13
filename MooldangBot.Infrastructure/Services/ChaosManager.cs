using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Interfaces;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [심연의 지배자]: 가상 장애 상태(Abyssal Trials)를 제어하고 타이머를 통해 자동 복구하는 서비스입니다.
/// </summary>
public class ChaosManager(ILogger<ChaosManager> logger) : IChaosManager
{
    private bool _isRedisPanic;
    private bool _isApiDelayed;
    private readonly object _lock = new();
    private Timer? _resetTimer;

    /// <summary>카오스 모드 활성화 여부</summary>
    public bool IsChaosEnabled 
    { 
        get { lock (_lock) return _isRedisPanic || _isApiDelayed; }
        set { if (!value) Reset(); } // [v18.3] 끌 때만 Reset() 연동 (켜는 건 개별 트리거 사용 권장)
    }
    public bool IsRedisPanic { get { lock (_lock) return _isRedisPanic; } }
    public bool IsApiDelayed { get { lock (_lock) return _isApiDelayed; } }

    public async Task TryInjectFaultAsync(string featureName)
    {
        // [v18.2] 심연의 속삭임: 특정 상황에서 인위적 장애 주입
        if (!IsChaosEnabled) return;

        lock (_lock)
        {
            if (_isApiDelayed)
            {
                logger.LogWarning("🌪️ [심연의 시련] 가상 API 지연 주입 중: {FeatureName}", featureName);
            }
            if (_isRedisPanic && featureName.Contains("Redis", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("🔥 [심연의 시련] 가상 Redis 장애 주입 중: {FeatureName}", featureName);
            }
        }

        if (IsApiDelayed)
        {
            await Task.Delay(Random.Shared.Next(500, 2000)); // 0.5s ~ 2s 랜덤 지연
        }
    }

    public void TriggerRedisPanic(TimeSpan? duration = null)
    {
        lock (_lock)
        {
            _isRedisPanic = true;
            logger.LogError("🔥 [심연의 시련] 가상 Redis 장애(Panic)가 활성화되었습니다.");
            ScheduleReset(duration ?? TimeSpan.FromMinutes(5));
        }
    }

    public void TriggerApiDelay(TimeSpan? duration = null)
    {
        lock (_lock)
        {
            _isApiDelayed = true;
            logger.LogWarning("🌪️ [심연의 시련] 가상 API 지연(Delay)이 활성화되었습니다.");
            ScheduleReset(duration ?? TimeSpan.FromMinutes(5));
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _isRedisPanic = false;
            _isApiDelayed = false;
            _resetTimer?.Dispose();
            _resetTimer = null;
            logger.LogInformation("✅ [심연의 시련] 모든 가상 장애 상태가 초기화되었으며, 함대의 평화가 찾아왔습니다.");
        }
    }

    private void ScheduleReset(TimeSpan duration)
    {
        _resetTimer?.Dispose();
        _resetTimer = new Timer(_ => Reset(), null, duration, Timeout.InfiniteTimeSpan);
    }
}
