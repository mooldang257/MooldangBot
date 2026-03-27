using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [오시리스의 기록관]: 실시간 채팅 데이터를 고속 집계하고 세션을 관리하는 실전 구현체입니다.
/// </summary>
public class BroadcastScribe(IAppDbContext db) : IBroadcastScribe
{
    // [기록관의 책상]: 메모리 내 실시간 집계 공간 (ChzzkUid -> Statistics)
    private static readonly ConcurrentDictionary<string, SessionStats> _activeStats = new();

    private class SessionStats
    {
        public int ChatCount { get; set; }
        public ConcurrentDictionary<string, int> Keywords { get; } = new();
        public ConcurrentDictionary<string, int> Emotes { get; } = new();
    }

    public void AddChatMessage(string chzzkUid, string message)
    {
        if (!_activeStats.TryGetValue(chzzkUid, out var stats)) return;

        stats.ChatCount++;

        // 1. [침묵 속의 미소]: 이모티콘 추출 (패턴: :{id}:)
        var emotes = Regex.Matches(message, @":([a-zA-Z0-9_]+):");
        foreach (Match match in emotes)
        {
            stats.Emotes.AddOrUpdate(match.Value, 1, (_, v) => v + 1);
        }

        // 2. [지식의 파편]: 단어 토큰화 (단순 공백 및 특수문자 제거)
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words.Where(w => w.Length > 1))
        {
            stats.Keywords.AddOrUpdate(word, 1, (_, v) => v + 1);
        }
    }

    public async Task<int> HeartbeatAsync(string chzzkUid)
    {
        var session = await db.BroadcastSessions
            .Where(s => s.ChzzkUid == chzzkUid && s.IsActive)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            // [새로운 서사]: 세션 신설
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
}
