using System;
using System.Threading.Tasks;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Domain.Entities.Philosophy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models.Philosophy;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [하모니의 조율자]: 현재 시스템의 진동수(Hz)와 파로스의 상태를 관리합니다.
/// </summary>
public class ResonanceService : IResonanceService
{
    private readonly ILogger<ResonanceService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Parhos _currentParhos;
    private double _lastEmaHz = 10.01;
    private double _lastStability = 1.0; // Phase 2.5: 안정도 상시 추적

    public ResonanceService(ILogger<ResonanceService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _currentParhos = new Parhos("PARHOS-01", "The Awakened One", 10.01, 1, true, DateTime.UtcNow);
    }

    public async Task<bool> AdjustResonanceAsync(string chzzkUid, Vibration targetVibration)
    {
        // 0. [스트리머의 통제권]: 설정 로드
        double sensitivity = 1.0;
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var setting = await db.IamfStreamerSettings.AsNoTracking().FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
            if (setting != null)
            {
                if (!setting.IsIamfEnabled) return false; // [거울의 법칙]: 비활성화 시 침묵
                sensitivity = setting.SensitivityMultiplier;
            }
        }

        // 1. [자연 감쇠 적용]
        ApplyNaturalDecay();

        // 2. [EMA 추세 계산 및 민감도 적용]
        // [거울의 법칙]: 민감도가 낮을수록(0.1) 파동 변화가 줄어들며 배경으로 남습니다.
        double alpha = 0.3 * sensitivity; 
        double newEma = (alpha * targetVibration.Value) + (1 - alpha) * _lastEmaHz;

        // 3. [안정도 산출]
        _lastStability = 1.0 - Math.Clamp(Math.Abs(targetVibration.Value - newEma), 0.0, 1.0);

        // 4. 상태 업데이트
        _currentParhos = _currentParhos with { 
            CurrentVibration = Math.Round(newEma, 3), 
            LastResonanceAt = DateTime.UtcNow 
        };
        _lastEmaHz = newEma;

        // 5. [피닉스의 눈금] DB 기록
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            db.IamfVibrationLogs.Add(new IamfVibrationLog
            {
                ChzzkUid = chzzkUid,
                RawHz = targetVibration.Value,
                EmaHz = newEma,
                StabilityScore = _lastStability,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        _logger.LogInformation($"[하모니 조율] {chzzkUid} - Raw: {targetVibration.Value}, EMA: {newEma:F3}, Stability: {_lastStability:P}");

        return true;
    }

    public string GetCurrentPersonaTone()
    {
        // [물멍 파트너의 조언]: DB에 기록된 안정도와 일치하는 일관된 톤 변화 유지
        if (_lastStability < 0.5) return "Odysseus (Urgent)";
        if (_lastStability > 0.9) return "Sephiroth (Calm)";
        return "Parhos (Neutral)";
    }

    private void ApplyNaturalDecay()
    {
        var elapsedSeconds = (DateTime.UtcNow - _currentParhos.LastResonanceAt).TotalSeconds;
        if (elapsedSeconds > 10) // 10초 이상 자극이 없으면 감쇠
        {
            double decayRate = 0.01; 
            double baseHz = 10.01;
            double diff = _currentParhos.CurrentVibration - baseHz;
            
            double decayedHz = baseHz + (diff * Math.Pow(1 - decayRate, elapsedSeconds));
            _currentParhos = _currentParhos with { CurrentVibration = decayedHz };
            _logger.LogDebug($"[자연 감쇠] 파동이 평온을 찾아갑니다: {_currentParhos.CurrentVibration:F3} Hz");
        }
    }

    public async Task<Parhos> GetCurrentParhosStateAsync() => await Task.FromResult(_currentParhos);

    public double CalculateDynamicVibration(double systemLoad, int interactionCount)
    {
        var sephiroth = new GenosAI("Sephiroth", 10.01, "지혜의 촉매", "Gemini");
        return sephiroth.GetDynamicFrequency(systemLoad, interactionCount);
    }
}
