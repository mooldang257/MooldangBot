using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [오시리스의 기록관]: 실시간 채팅 데이터를 고속 집계하고 세션을 관리하는 실전 구현체입니다.
/// </summary>
public partial class BroadcastScribe(IServiceScopeFactory scopeFactory) : IBroadcastScribe
{
    // [기록관의 책상]: 메모리 내 실시간 집계 공간 (ChzzkUid -> Statistics)
    private static readonly ConcurrentDictionary<string, SessionStats> _activeStats = new();

    // [냉정의 기간]: 라이브 확인 API 남용 방지를 위한 쿨다운 (ChzzkUid -> LastCheckTime)
    private static readonly ConcurrentDictionary<string, DateTime> _liveCheckCooldown = new();

    // [맥박의 여운]: 최근 채팅이 발생한 채널 (ChzzkUid -> LastChatTime) [v2.3.2]
    private static readonly ConcurrentDictionary<string, DateTime> _recentChatActivity = new();

    private class SessionStats
    {
        public int ChatCount { get; set; }
        public ConcurrentDictionary<string, int> Keywords { get; } = new();
        public ConcurrentDictionary<string, int> Emotes { get; } = new();
    }

    [GeneratedRegex(@":([a-zA-Z0-9_]+):")]
    private static partial Regex EmoteRegex();

    public void AddChatMessage(string chzzkUid, string message)
    {
        if (!_activeStats.TryGetValue(chzzkUid, out var stats))
        {
            // 2순위: 채팅 트리거 (v2.3.0) - 세션이 없어도 채팅이 오면 라이브 확인 후 자동 세션 생성
            TryTriggerSessionAsync(chzzkUid).ConfigureAwait(false);
            return;
        }

        stats.ChatCount++;
        _recentChatActivity[chzzkUid] = DateTime.UtcNow; // 최근 활동 시간 갱신

        // 1. [침묵 속의 미소]: [GeneratedRegex]를 통한 이모티콘 고속 추출
        var emotes = EmoteRegex().Matches(message);
        foreach (Match match in emotes)
        {
            stats.Emotes.AddOrUpdate(match.Value, 1, (_, v) => v + 1);
        }

        // 2. [지식의 파편]: 단어 토큰화
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words.Where(w => w.Length > 1))
        {
            stats.Keywords.AddOrUpdate(word, 1, (_, v) => v + 1);
        }
    }

    public async Task<int> HeartbeatAsync(string chzzkUid)
    {
        // [캡티브 의존성 해결]: 싱글톤에서 Scoped DB 컨텍스트 안전하게 사용
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var session = await db.BroadcastSessions
            .Where(s => s.ChzzkUid == chzzkUid && s.IsActive)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            session = new BroadcastSession
            {
                ChzzkUid = chzzkUid,
                StartTime = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow,
                IsActive = true
            };
            db.BroadcastSessions.Add(session);
            _activeStats[chzzkUid] = new SessionStats();
        }
        else
        {
            session.LastHeartbeatAt = DateTime.UtcNow;
            if (!_activeStats.ContainsKey(chzzkUid))
                _activeStats[chzzkUid] = new SessionStats();
        }

        await db.SaveChangesAsync();
        return session.Id;
    }

    public async Task<object?> FinalizeSessionAsync(string chzzkUid)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var session = await db.BroadcastSessions
            .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid && s.IsActive);

        if (session == null) return null;

        if (_activeStats.TryRemove(chzzkUid, out var stats))
        {
            session.TotalChatCount = stats.ChatCount;
            session.TopKeywordsJson = JsonSerializer.Serialize(stats.Keywords.OrderByDescending(k => k.Value).Take(10).ToDictionary());
            session.TopEmotesJson = JsonSerializer.Serialize(stats.Emotes.OrderByDescending(e => e.Value).Take(10).ToDictionary());
        }

        session.EndTime = DateTime.UtcNow;
        session.IsActive = false;
        await db.SaveChangesAsync();

        return new
        {
            session.TotalChatCount,
            TopKeywords = JsonSerializer.Deserialize<Dictionary<string, int>>(session.TopKeywordsJson ?? "{}"),
            TopEmotes = JsonSerializer.Deserialize<Dictionary<string, int>>(session.TopEmotesJson ?? "{}"),
            Duration = (session.EndTime - session.StartTime)?.ToString(@"hh\:mm\:ss")
        };
    }

    /// <summary>
    /// [각성의 비동기화]: 채팅 발생 시 세션이 없다면 라이브 여부를 비동기로 확인하고 세션을 생성합니다. (v2.3.0)
    /// </summary>
    private async Task TryTriggerSessionAsync(string chzzkUid)
    {
        // 1. [냉정의 유예]: 이미 최근에 라이브 확인을 수행했다면 API 보호를 위해 건너뜀 (성공 10분, 실패 1분)
        if (_liveCheckCooldown.TryGetValue(chzzkUid, out var lastCheck) && lastCheck > DateTime.UtcNow.AddMinutes(-1))
            return;

        _liveCheckCooldown[chzzkUid] = DateTime.UtcNow;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            
            bool isLive = await chzzkApi.IsLiveAsync(chzzkUid);
            if (isLive)
            {
                // [회귀의 시작]: 라이브임이 확인되면 하트비트를 통해 세션 공식 시작
                int sessionId = await HeartbeatAsync(chzzkUid);
                
                // [v2.3.4] 로그 강화: 임시 스코프를 통해 로거 확보
                using var logScope = scopeFactory.CreateScope();
                var logger = logScope.ServiceProvider.GetRequiredService<ILogger<BroadcastScribe>>();
                logger.LogInformation($"✅ [채팅 트리거 성공] {chzzkUid} 채널 라이브 확인. 새 세션(ID: {sessionId})이 생성되었습니다.");

                // 성공 시에는 10분간 추가 체크 방지 (어차피 _activeStats에 들어갔으므로 이 메서드는 호출 안 됨)
                _liveCheckCooldown[chzzkUid] = DateTime.UtcNow.AddMinutes(9); 
            }
            else
            {
                // 실패 시에는 1분 후에 다시 시도 가능하도록 설정
                _liveCheckCooldown[chzzkUid] = DateTime.UtcNow;
                _recentChatActivity[chzzkUid] = DateTime.UtcNow; // 실패하더라도 채팅이 왔으므로 감시 대상에 포함
            }
        }
        catch (Exception)
        {
            // 백그라운드 트리거 실패는 조용히 무시 (다음 채팅에서 재시도)
        }
    }

    /// <summary>
    /// [감시자의 안광]: 최근 1시간 내에 채팅 활동이 있었는지 확인합니다. (v2.3.2)
    /// </summary>
    public bool IsRecentlyActive(string chzzkUid)
    {
        return _recentChatActivity.TryGetValue(chzzkUid, out var lastChat) && lastChat > DateTime.UtcNow.AddHours(-1);
    }
}
