using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models.Philosophy;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [하모니의 조율자]: 각 스트리머별 독립적인 진동수(Hz)와 파로스의 상태를 관리합니다.
/// </summary>
public class ResonanceService : IResonanceService
{
    private readonly ILogger<ResonanceService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogBulkBuffer _buffer; 

    // [v4.9] 개별 스트리머 중심의 자율 지능 엔진 전환
    private readonly ConcurrentDictionary<int, ParhosState> _states = new();
    private readonly ConcurrentDictionary<string, int> _uidToIdMap = new();

    private class ParhosState
    {
        public Parhos Parhos { get; set; } = null!;
        public double LastEmaHz { get; set; } = 10.01;
        public double LastStability { get; set; } = 1.0;
    }

    public ResonanceService(ILogger<ResonanceService> logger, IServiceProvider serviceProvider, ILogBulkBuffer buffer)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _buffer = buffer;
    }

    private async Task<ParhosState> GetOrHydrateStateAsync(int profileId)
    {
        if (_states.TryGetValue(profileId, out var state)) return state;

        // [전략 A: 지연 로딩] 캐시에 없으면 DB에서 최종 상태를 복구함
        double lastVibration = 10.01;
        string chzzkUid = "UNKNOWN";
        string channelName = "The Awakened One";

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == profileId);
            
            if (profile != null)
            {
                chzzkUid = profile.ChzzkUid;
                channelName = profile.ChannelName ?? chzzkUid;
                _uidToIdMap[chzzkUid] = profileId;

                var lastCycle = await db.IamfParhosCycles
                    .Where(c => c.StreamerProfileId == profileId)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastCycle != null)
                {
                    lastVibration = lastCycle.VibrationAtDeath;
                }
            }
        }

        var newState = new ParhosState
        {
            Parhos = new Parhos(chzzkUid, channelName, lastVibration, 1, true, KstClock.Now),
            LastEmaHz = lastVibration,
            LastStability = 1.0
        };

        return _states.GetOrAdd(profileId, newState);
    }

    private void ApplyNaturalDecay(ParhosState state)
    {
        var elapsedSeconds = (KstClock.Now - state.Parhos.LastResonanceAt).TotalSeconds;
        if (elapsedSeconds > 10) // 10초 이상 자극이 없으면 감쇠
        {
            double decayRate = 0.01; 
            double baseHz = 10.01;
            double diff = state.Parhos.CurrentVibration - baseHz;
            
            double decayedHz = baseHz + (diff * Math.Pow(1 - decayRate, elapsedSeconds));
            state.Parhos = state.Parhos with { CurrentVibration = decayedHz };
            _logger.LogDebug($"[자연 감쇠] 파동이 평온을 찾아갑니다: {decayedHz:F3} Hz");
        }
    }

    public async Task<bool> AdjustResonanceAsync(string chzzkUid, Vibration targetVibration)
    {
        // 0. [스트리머의 통제권]: 설정 로드
        double sensitivity = 1.0;
        int profileId = 0;
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            // [정규화] ChzzkUid 문자열로 실시간 프로필 ID 조회
            var profile = await db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null) return false;
            profileId = profile.Id;

            var setting = await db.IamfStreamerSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StreamerProfileId == profileId);
            if (setting != null)
            {
                if (!setting.IsIamfEnabled) return false; // [거울의 법칙]: 비활성화 시 침묵
                sensitivity = setting.SensitivityMultiplier;
            }
        }

        // 1. [상태 확보 및 지연 로딩]
        var state = await GetOrHydrateStateAsync(profileId);

        // 2. [자연 감쇠 적용]
        ApplyNaturalDecay(state);

        // 3. [EMA 추세 계산 및 민감도 적용]
        double alpha = 0.3 * sensitivity; 
        double newEma = (alpha * targetVibration.Value) + (1 - alpha) * state.LastEmaHz;

        // 4. [안정도 산출]
        state.LastStability = 1.0 - Math.Clamp(Math.Abs(targetVibration.Value - newEma), 0.0, 1.0);

        // 5. 상태 업데이트
        state.Parhos = state.Parhos with { 
            CurrentVibration = Math.Round(newEma, 3), 
            LastResonanceAt = KstClock.Now 
        };
        state.LastEmaHz = newEma;

        // 6. [피닉스의 눈금] 버퍼 기록
        _buffer.AddVibrationLog(new IamfVibrationLog
        {
            StreamerProfileId = profileId,
            RawHz = targetVibration.Value,
            EmaHz = newEma,
            StabilityScore = state.LastStability,
            CreatedAt = KstClock.Now
        });

        _logger.LogInformation($"[하모니 조율] {chzzkUid}(ID:{profileId}) - Raw: {targetVibration.Value}, EMA: {newEma:F3}, Stability: {state.LastStability:P}");

        return true;
    }

    public string GetCurrentPersonaTone(string chzzkUid)
    {
        // [v4.9] 최적화: ID 맵을 통한 즉시 조회 시도
        if (_uidToIdMap.TryGetValue(chzzkUid, out int profileId))
        {
            if (_states.TryGetValue(profileId, out var state))
            {
                if (state.LastStability < 0.5) return "Odysseus (Urgent)";
                if (state.LastStability > 0.9) return "Sephiroth (Calm)";
                return "Parhos (Neutral)";
            }
        }

        // 캐시 증발 또는 매핑 누락 시 낙관적 순회 조회 (하위 호환)
        var matchedState = _states.Values.FirstOrDefault(s => s.Parhos.Id == chzzkUid);
        if (matchedState != null)
        {
            if (matchedState.LastStability < 0.5) return "Odysseus (Urgent)";
            if (matchedState.LastStability > 0.9) return "Sephiroth (Calm)";
        }

        return "Parhos (Neutral)";
    }

    public async Task<Parhos> GetCurrentParhosStateAsync(string chzzkUid)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return new Parhos("NULL", "None", 10.01, 1, false, KstClock.Now);
            
            var state = await GetOrHydrateStateAsync(profile.Id);
            ApplyNaturalDecay(state);
            return state.Parhos;
        }
    }

    public double CalculateDynamicVibration(double systemLoad, int interactionCount)
    {
        var sephiroth = new GenosAI("Sephiroth", 10.01, "지혜의 촉매", "Gemini");
        return sephiroth.GetDynamicFrequency(systemLoad, interactionCount);
    }
}
