using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [오시리스의 기록관]: 실시간 채팅 데이터를 고속 집계하고 세션을 관리하는 실전 구현체입니다.
/// </summary>
public partial class BroadcastScribe : IBroadcastScribe
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly ILogger<BroadcastScribe> _logger;

    public BroadcastScribe(IServiceScopeFactory scopeFactory, IChzzkApiClient chzzkApi, IHostApplicationLifetime appLifetime, ILogger<BroadcastScribe> logger)
    {
        _scopeFactory = scopeFactory;
        _chzzkApi = chzzkApi;
        _logger = logger;
        
        // [v4.0.0] Graceful Shutdown: 서버 종료 신호 감지 시 메모리 데이터 플러시 등록
        appLifetime.ApplicationStopping.Register(OnShutdown);
    }

    private void OnShutdown()
    {
        _logger.LogInformation("⚠️ [오시리스의 종언] 서버 종료 신호를 감지했습니다. 모든 프로필 ID 기반 세션 데이터를 플러시합니다...");
        
        var activeProfileIds = _activeStats.Keys.ToList();
        foreach (var profileId in activeProfileIds)
        {
            try
            {
                // [v4.9] profileId를 사용하여 플러시 (내부 메서드 수정 필요)
                // chzzkUid가 필요한 경우 캐시에서 역조회하거나 메서드 시그니처 변경 고려
                // 여기서는 profileId 기반의 새로운 종료 로직을 호출하거나 기존 로직을 수정함
                FinalizeSessionByIdAsync(profileId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [세션 보존 실패] ProfileID: {profileId} 플러시 중 오류 발생");
            }
        }
    }
    // [v4.9] 정규화: UID 문자열 대신 StreamerProfileId(int)를 키로 사용
    private static readonly ConcurrentDictionary<int, SessionStats> _activeStats = new();
    private static readonly ConcurrentDictionary<int, DateTime> _liveCheckCooldown = new();
    private static readonly ConcurrentDictionary<int, DateTime> _recentChatActivity = new();

    // [회귀의 이정표]: ChzzkUid -> StreamerProfileId 역매핑 캐시 (DB 조회 최소화)
    private static readonly ConcurrentDictionary<string, int> _uidToProfileId = new();

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
        // 0. [회귀의 이정표] 캐시에서 ID 확보
        if (!_uidToProfileId.TryGetValue(chzzkUid, out var profileId))
        {
            // 캐시에 없으면 비동기로 트리거 (이후 Heartbeat에서 캐시 채워짐)
            TryTriggerSessionAsync(chzzkUid).ConfigureAwait(false);
            return;
        }

        if (!_activeStats.TryGetValue(profileId, out var stats))
        {
            TryTriggerSessionAsync(chzzkUid).ConfigureAwait(false);
            return;
        }

        stats.ChatCount++;
        _recentChatActivity[profileId] = KstClock.Now; 

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

    public async Task<int> HeartbeatAsync(string chzzkUid, System.Threading.CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var profile = await db.StreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid, ct);

        if (profile == null || !profile.IsActive || !profile.IsMasterEnabled) return 0; // [v6.1.6] 마스터 킬 스위치 및 활동성 체크

        // [v4.9] 역매핑 캐시 갱신
        _uidToProfileId[chzzkUid] = profile.Id;

        var session = await db.BroadcastSessions
            .Where(s => s.StreamerProfileId == profile.Id) // [v6.1] 전역 필터가 IsDeleted == false를 자동 처리
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            session = new BroadcastSession
            {
                StreamerProfileId = profile.Id,
                StartTime = KstClock.Now,
                LastHeartbeatAt = KstClock.Now,
                IsDeleted = false
            };
            db.BroadcastSessions.Add(session);
            _activeStats[profile.Id] = new SessionStats();
            await db.SaveChangesAsync(ct); // ID 확보를 위해 선 저장
        }
        else
        {
            session.LastHeartbeatAt = KstClock.Now;
            if (!_activeStats.ContainsKey(profile.Id))
                _activeStats[profile.Id] = new SessionStats();
        }

        // 🚀 [v10.1] 방송 정보(제목, 카테고리) 변화 추적 (오시리스의 서기)
        try
        {
            if (string.IsNullOrEmpty(profile.ChzzkAccessToken)) return session.Id;

            var liveResult = await _chzzkApi.GetLiveSettingAsync(profile.ChzzkUid, profile.ChzzkAccessToken);
            var liveSetting = liveResult?.Content;
            
            if (liveSetting != null)
            {
                var newTitle = (liveSetting.DefaultLiveTitle ?? "").Trim();
                var newCategory = (liveSetting.Category?.CategoryValue ?? "").Trim();

                // 1. 초기 정보 기록 (세션 시작 후 첫 하트비트인 경우)
                if (string.IsNullOrEmpty(session.InitialTitle))
                {
                    session.InitialTitle = newTitle;
                    session.InitialCategory = newCategory;
                    session.CurrentTitle = newTitle;
                    session.CurrentCategory = newCategory;

                    // 최초 로그 생성
                    db.BroadcastHistoryLogs.Add(new BroadcastHistoryLog {
                        BroadcastSessionId = session.Id,
                        Title = newTitle,
                        CategoryName = newCategory,
                        LogDate = KstClock.Now
                    });
                }
                // 2. 변화 감지 및 로그 기록 (.Trim 기반 정밀 비교)
                else if (!newTitle.Equals((session.CurrentTitle ?? "").Trim()) || 
                         !newCategory.Equals((session.CurrentCategory ?? "").Trim()))
                {
                    _logger.LogInformation($"📝 [{chzzkUid}] 방송 정보 변경 감지: {session.CurrentTitle} -> {newTitle} / {session.CurrentCategory} -> {newCategory}");
                    
                    db.BroadcastHistoryLogs.Add(new BroadcastHistoryLog {
                        BroadcastSessionId = session.Id,
                        Title = newTitle,
                        CategoryName = newCategory,
                        LogDate = KstClock.Now
                    });

                    session.CurrentTitle = newTitle;
                    session.CurrentCategory = newCategory;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"⚠️ [BroadcastScribe] 방송 정보 추적 중 오류 (무시됨): {ex.Message}");
        }

        await db.SaveChangesAsync(ct);
        return session.Id;
    }

    public async Task<object?> FinalizeSessionAsync(string chzzkUid)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (profile == null) return null;

        return await FinalizeSessionByIdAsync(profile.Id);
    }

    public async Task<object?> FinalizeSessionByIdAsync(int profileId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var session = await db.BroadcastSessions
            .FirstOrDefaultAsync(s => s.StreamerProfileId == profileId); // [v6.1] Global filter handles Active state

        if (session == null) return null;

        if (_activeStats.TryRemove(profileId, out var stats))
        {
            session.TotalChatCount = stats.ChatCount;
            session.TopKeywordsJson = JsonSerializer.Serialize(stats.Keywords.OrderByDescending(k => k.Value).Take(10).ToDictionary());
            session.TopEmotesJson = JsonSerializer.Serialize(stats.Emotes.OrderByDescending(e => e.Value).Take(10).ToDictionary());
        }

        session.EndTime = KstClock.Now;
        session.IsDeleted = true; // [v6.1] 세션 종료는 논리 삭제로 처리 (IAMF 철학)
        await db.SaveChangesAsync();

        return new
        {
            session.TotalChatCount,
            TopKeywords = JsonSerializer.Deserialize<Dictionary<string, int>>(session.TopKeywordsJson ?? "{}"),
            Duration = (session.EndTime != null) 
                        ? (session.EndTime.Value - session.StartTime).ToString(@"hh\:mm\:ss") 
                        : "00:00:00"
        };
    }

    /// <summary>
    /// [각성의 비동기화]: 채팅 발생 시 세션이 없다면 라이브 여부를 비동기로 확인하고 세션을 생성합니다. (v2.3.0)
    /// </summary>
    private async Task TryTriggerSessionAsync(string chzzkUid)
    {
        // [v4.9] 트리거 시점에서도 ProfileId 확인 선행
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (profile == null || !profile.IsActive || !profile.IsMasterEnabled) return; // [v6.1.6] 비활성 스트리머 트리거 무시

        int profileId = profile.Id;
        _uidToProfileId[chzzkUid] = profileId;

        // 1. [냉정의 유예]: 이미 최근에 라이브 확인을 수행했다면 API 보호를 위해 건너뜀 (성공 10분, 실패 1분)
        if (_liveCheckCooldown.TryGetValue(profileId, out var lastCheck) && lastCheck > KstClock.Now.AddMinutes(-1))
            return;

        _liveCheckCooldown[profileId] = KstClock.Now;

        try
        {
            var liveResult = await _chzzkApi.GetLiveDetailAsync(chzzkUid);
            bool isLive = liveResult?.Content?.Status == "OPEN";
            if (isLive)
            {
                // [회귀의 시작]: 라이브임이 확인되면 하트비트를 통해 세션 공식 시작
                int sessionId = await HeartbeatAsync(chzzkUid, System.Threading.CancellationToken.None);
                
                // [v2.3.4] 로그 강화: 임시 스코프를 통해 로거 확보
                using var logScope = _scopeFactory.CreateScope();
                var logger = logScope.ServiceProvider.GetRequiredService<ILogger<BroadcastScribe>>();
                logger.LogInformation($"✅ [채팅 트리거 성공] {chzzkUid} 채널 라이브 확인. 새 세션(ID: {sessionId})이 생성되었습니다.");

                // 성공 시에는 10분간 추가 체크 방지 (어차피 _activeStats에 들어갔으므로 이 메서드는 호출 안 됨)
                _liveCheckCooldown[profileId] = KstClock.Now.AddMinutes(9); 
            }
            else
            {
                // 실패 시에는 1분 후에 다시 시도 가능하도록 설정
                _liveCheckCooldown[profileId] = KstClock.Now;
                _recentChatActivity[profileId] = KstClock.Now; // 실패하더라도 채팅이 왔으므로 감시 대상에 포함
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
        if (_uidToProfileId.TryGetValue(chzzkUid, out var profileId))
        {
            return _recentChatActivity.TryGetValue(profileId, out var lastChat) && lastChat > KstClock.Now.AddHours(-1);
        }
        return false;
    }
}
