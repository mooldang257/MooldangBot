using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Common.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;

using MooldangBot.Domain.Common;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [오시리스의 감시소]: 시스템의 생존 지표와 IAMF 엔진 상태를 집계하는 관리자용 API입니다.
/// </summary>
[ApiController]
[Route("api/admin/system-health")]
public class AdminStatusController(
    IChzzkChatClient chatClient,
    ITokenRenewalService renewalService,
    IHealthMonitorService healthMonitor,
    IChaosManager chaos) : ControllerBase
{
    [HttpGet]
    public IActionResult GetSystemHealth()
    {
        // [인프라의 평정심]: 시스템 메트릭 수집
        var process = Process.GetCurrentProcess();
        var memoryMb = process.WorkingSet64 / (1024 * 1024);
        
        // 가상 데이터: 실제 진동수 수집 로직 연동 가능
        var avgVibration = 10.01; 
        
        return Ok(new
        {
            TotalActiveBots = chatClient.GetActiveConnectionCount(),
            MemoryUsage = $"{memoryMb} MB",
            IsCircuitOpen = renewalService.IsCircuitOpen(),
            Uptime = (KstClock.Now - process.StartTime).ToString(@"dd\.hh\:mm\:ss"),
            AvgVibration = $"{avgVibration:F2} Hz",
            Timestamp = KstClock.Now.ToString("O")
        });
    }

    /// <summary>
    /// [심연의 맥박]: 모든 인프라와 워커의 상세 건강 상태를 조회합니다.
    /// </summary>
    [HttpGet("pulse")]
    public async Task<IActionResult> GetSystemPulse(CancellationToken ct)
    {
        var pulse = await healthMonitor.GetSystemPulseAsync(ct);
        return Ok(pulse);
    }

    /// <summary>
    /// [혼돈의 도래]: 시스템에 인위적인 장애 시뮬레이션을 On/Off 합니다. (관리자 전용)
    /// </summary>
    [HttpPost("chaos/toggle")]
    public IActionResult ToggleChaos([FromQuery] bool enabled)
    {
        chaos.IsChaosEnabled = enabled;
        var status = enabled ? "가동 (시련의 시작)" : "중단 (이지스의 안식)";
        return Ok(new { ChaosEnabled = enabled, Message = $"⛈️ [카오스 시뮬레이터] {status}" });
    }

    [HttpGet("logs")]
    public IActionResult GetRecentLogs()
    {
        // [서기의 최종 기록]: 최신 로그 샘플 반환 (실전에서는 DB의 Logs 테이블 조회)
        return Ok(new[] {
            new { Time = KstClock.Now.AddSeconds(-10).ToString("HH:mm:ss"), Level = "INFO", Msg = "[피닉스] 세션 상태 양호" },
            new { Time = KstClock.Now.AddSeconds(-30).ToString("HH:mm:ss"), Level = "WARN", Msg = "[와치독] 토큰 임박 감지됨" }
        });
    }
}
