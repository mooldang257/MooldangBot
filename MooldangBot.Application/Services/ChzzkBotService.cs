using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Services;

public class ChzzkBotService : IChzzkBotService
{
    private readonly IAppDbContext _db;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly IChzzkChatClient _chatClient;
    private readonly IDynamicQueryEngine _dynamicEngine; // [v1.9]
    private readonly ITokenRenewalService _renewalService; // [v13.1] 엔진 일원화
    private readonly ILogger<ChzzkBotService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // [v5.7] 자가 치유를 위한 파수꾼들 (정적 복구 잠금 및 지능형 쿨다운)
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _recoveryLocks = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastRecoveryAttempts = new();
    private static readonly ConcurrentDictionary<string, int> _recoveryRetryCounts = new();
    
    private const int MaxAutoRecoversPerWindow = 3;
    private static readonly TimeSpan RecoveryCooldown = TimeSpan.FromMinutes(1);

    public ChzzkBotService(
        IAppDbContext db, 
        IChzzkApiClient chzzkApi, 
        IChzzkChatClient chatClient, 
        IDynamicQueryEngine dynamicEngine, 
        ITokenRenewalService renewalService,
        IServiceScopeFactory scopeFactory,
        ILogger<ChzzkBotService> logger)
    {
        _db = db;
        _chzzkApi = chzzkApi;
        _chatClient = chatClient;
        _dynamicEngine = dynamicEngine;
        _renewalService = renewalService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<string?> GetBotTokenAsync(StreamerProfile profile)
    {
        // 🥇 1순위: 스트리머 커스텀 봇 계정 확인 및 갱신
        if (!string.IsNullOrEmpty(profile.BotRefreshToken))
        {
            if (!string.IsNullOrEmpty(profile.BotAccessToken) &&
                profile.BotTokenExpiresAt.HasValue &&
                profile.BotTokenExpiresAt.Value > DateTime.UtcNow.AddHours(9).AddHours(1))
            {
                return profile.BotAccessToken;
            }

            _logger.LogWarning($"🔄 [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 만료 임박! 자동 갱신 시도...");

            var customTokenRes = await _chzzkApi.RefreshTokenAsync(profile.BotRefreshToken);

            if (customTokenRes != null && customTokenRes.Code == 200 && customTokenRes.Content != null)
            {
                var content = customTokenRes.Content;
                profile.BotAccessToken = content.AccessToken;
                profile.BotRefreshToken = content.RefreshToken ?? profile.BotRefreshToken;
                profile.BotTokenExpiresAt = DateTime.UtcNow.AddHours(9).AddSeconds(content.ExpiresIn);

                await _db.SaveChangesAsync();
                _logger.LogInformation($"✅ [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 갱신 성공!");
                return profile.BotAccessToken;
            }
            else
            {
                _logger.LogError($"❌ [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 갱신 실패! 기본 봇으로 폴백(Fallback)합니다.");
            }
        }

        // 🥈 2순위: 시스템 공통 봇 계정 확인 및 갱신 (SystemSettings)
        var botKeys = new List<string> { "BotAccessToken", "BotRefreshToken", "BotTokenExpiresAt" };
        var globalSettings = await _db.SystemSettings.Where(s => botKeys.Contains(s.KeyName)).ToListAsync();
        var globalTokenSetting = globalSettings.FirstOrDefault(s => s.KeyName == "BotAccessToken");
        var globalRefreshSetting = globalSettings.FirstOrDefault(s => s.KeyName == "BotRefreshToken");
        var globalExpiresSetting = globalSettings.FirstOrDefault(s => s.KeyName == "BotTokenExpiresAt");

        string? globalToken = globalTokenSetting?.KeyValue;
        string? globalRefresh = globalRefreshSetting?.KeyValue;
        DateTime globalExpireDate = DateTime.MinValue;

        // [v13.1] 모든 비교를 KST 강제 (UTC+9)
        var kstNow = DateTime.UtcNow.AddHours(9);

        if (!string.IsNullOrEmpty(globalToken) && globalExpireDate > kstNow.AddHours(1))
        {
            return globalToken;
        }

        if (!string.IsNullOrEmpty(globalRefresh))
        {
            _logger.LogWarning("🔄 [물댕봇] 시스템 공통 봇 토큰 만료 임박! 자동 갱신 시도...");

            var globalTokenRes = await _chzzkApi.RefreshTokenAsync(globalRefresh);

            if (globalTokenRes != null && globalTokenRes.Code == 200 && globalTokenRes.Content != null)
            {
                var content = globalTokenRes.Content;
                string newAccess = content.AccessToken ?? "";
                string newRefresh = content.RefreshToken ?? globalRefresh;
                DateTime newExpire = DateTime.UtcNow.AddHours(9).AddSeconds(content.ExpiresIn);

                UpdateOrAddSystemSetting("BotAccessToken", newAccess);
                UpdateOrAddSystemSetting("BotRefreshToken", newRefresh);
                UpdateOrAddSystemSetting("BotTokenExpiresAt", newExpire.ToString("O"));

                await _db.SaveChangesAsync();
                _logger.LogInformation("✅ [물댕봇] 시스템 공통 봇 토큰 갱신 성공!");
                return newAccess;
            }
            else
            {
                _logger.LogError("❌ [물댕봇] 시스템 공통 봇 갱신마저 실패했습니다!");
            }
        }

        // 🥉 3순위: 최후의 수단 (스트리머 본인 토큰 반환)
        _logger.LogWarning($"⚠️ [물댕봇] 사용 가능한 봇 토큰이 없어 {profile.ChzzkUid}님의 방송용 계정으로 채팅을 보냅니다.");
        return profile.ChzzkAccessToken;
    }

    public async Task<bool> SendReplyChatAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token)
    {
        return await SendGenericChatAsync(profile, message, viewerUid, false, token);
    }

    public async Task<bool> SendReplyNoticeAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token)
    {
        return await SendGenericChatAsync(profile, message, viewerUid, true, token);
    }

    public async Task<string?> GetStreamerTokenAsync(StreamerProfile profile)
    {
        // [v13.1] 통합 갱신 엔진(ITokenRenewalService)으로 일원화
        await _renewalService.RenewIfNeededAsync(profile.ChzzkUid);
        
        // 갱신 후 최신 상태의 토큰 반환 (RenewIfNeeded 내부에서 SaveChanges 수행함)
        return profile.ChzzkAccessToken;
    }

    private async Task<bool> SendGenericChatAsync(StreamerProfile profile, string message, string viewerUid, bool isNotice, CancellationToken token)
    {
        try
        {
            // 🏷️ [v1.9.2] 봇의 응답이 시청자 채팅보다 너무 빨라지는 것을 방지하기 위해 0.1초 지연 추가
            await Task.Delay(100, token);

            // 🏷️ [v1.9] 전역 동적 쿼리 엔진 적용 (Service Layer 필터)
            string processedMessage = await _dynamicEngine.ProcessMessageAsync(message, profile.ChzzkUid, viewerUid);

            // ⭐ 계정 설정에 맞는 적절한 토큰 확보 (필요시 갱신 포함)
            string? tokenToUse = await GetBotTokenAsync(profile);
            if (string.IsNullOrEmpty(tokenToUse))
            {
                _logger.LogError($"❌ [ChzzkBotService] {profile.ChzzkUid} 유효한 발송 토큰을 찾을 수 없습니다.");
                return false;
            }

            _logger.LogInformation($"📡 [봇 채팅 발송] 대상채널: {profile.ChzzkUid}, 타입: {(isNotice ? "상단공지" : "일반")}");

            return isNotice 
                ? await _chzzkApi.SendChatNoticeAsync(tokenToUse, profile.ChzzkUid, processedMessage)
                : await _chzzkApi.SendChatMessageAsync(tokenToUse, profile.ChzzkUid, processedMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [ChzzkBotService] {profile.ChzzkUid} 채팅 발송 에러: {ex.Message}");
            return false;
        }
    }

    public async Task RefreshChannelAsync(string chzzkUid)
    {
        _logger.LogInformation($"🔄 [피닉스의 전령] {chzzkUid} 채널의 설정을 새로고침하고 연결을 점검합니다.");
        await EnsureConnectionAsync(chzzkUid);
    }

    public async Task EnsureConnectionAsync(string chzzkUid, bool forceFresh = false)
    {
        // [v16.3.2] 1. [영구 봉인 해제]: 30분이 지난 실패 기록은 자동으로 소멸(Decay)시킵니다.
        var kstNow = DateTime.UtcNow.AddHours(9);
        if (_recoveryRetryCounts.TryGetValue(chzzkUid, out var rc) && rc >= MaxAutoRecoversPerWindow)
        {
            if (_lastRecoveryAttempts.TryGetValue(chzzkUid, out var last) && kstNow - last > TimeSpan.FromMinutes(30))
            {
                _logger.LogInformation("⏳ [봉인 해제] {ChzzkUid} 채널의 실패 기록이 30분을 초과하여 복구 기회를 재생성합니다.", chzzkUid);
                CleanupRecoveryLock(chzzkUid);
            }
            else
            {
                _logger.LogDebug("[입구 가드] {ChzzkUid} 채널은 영구 복구 제한 상태입니다. (수동 리셋 필요)", chzzkUid);
                return;
            }
        }

        // [v8.0] 인증 에러 상태라면 자가 치유 핸들러로 위임 (와치독 연동)
        if (_chatClient.HasAuthError(chzzkUid))
        {
            await HandleAuthFailureAsync(chzzkUid);
            return;
        }

        // 2. [상태 확인]: 이미 연결되어 있다면 통과
        if (_chatClient.IsConnected(chzzkUid))
        {
            _logger.LogDebug($"[피닉스 점검] {chzzkUid} 세션이 이미 안정적으로 유지 중입니다.");
            return;
        }

        _logger.LogWarning($"[피닉스의 재건] {chzzkUid} 세션 단절 감지. 정화를 시작합니다.");
        await ConnectInternalAsync(chzzkUid, forceFresh);
    }

    private async Task ConnectInternalAsync(string chzzkUid, bool forceFresh)
    {
        try
        {
            // [v7.0] 하이브리드 전략: 자가 치유 시에는 캐싱을 무시하고 최신 데이터를, 평상시에는 캐싱된 데이터를 사용
            var query = _db.StreamerProfiles.AsQueryable();
            if (forceFresh) query = query.AsNoTracking();

            var profile = await query.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null || !profile.IsBotEnabled)
            {
                _logger.LogWarning($"[피닉스 중단] {chzzkUid} 봇이 존재하지 않거나 비활성화되었습니다.");
                return;
            }

            // 3. [토큰 상태 확인 및 갱신]: 401 인증 오류 방지를 위해 연결 전 토큰 체크
            string? accessToken = await GetStreamerTokenAsync(profile);
            
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError($"[파동의 오류] {chzzkUid} 인증 정보 확보에 최종 실패했습니다. 자가 치유 엔진을 가동합니다.");
                
                // [v11.2] 토큰 확보 실패 시 자가 치유 핸들러를 강제 호출하여 
                // 리프레시 토큰 상 태까지 정밀 진단하고 무한 루프를 차단합니다.
                await HandleAuthFailureAsync(chzzkUid);
                return;
            }
            
            // 4. [유기적 복구]: 기존 좀비 자원 정리 후 재연결
            await _chatClient.DisconnectAsync(chzzkUid);
            
            bool success = await _chatClient.ConnectAsync(chzzkUid, accessToken);

            if (success)
            {
                _logger.LogInformation($"✅ [피닉스 부활] {chzzkUid} 세션이 성공적으로 재건되었습니다.");
            }
            else
            {
                _logger.LogError($"❌ [피닉스 실패] {chzzkUid} 세션 재건에 실패했습니다.");
                
                // [v12.0] 연결 실패 후 인증 에러가 감지되었다면 즉시 자가 치유 실행
                if (_chatClient.HasAuthError(chzzkUid))
                {
                    await HandleAuthFailureAsync(chzzkUid);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [피닉스 예외] {chzzkUid} 복구 중 예기치 못한 오류 발생");
        }
    }

    public async Task HandleAuthFailureAsync(string chzzkUid)
    {
        // 1. [v5.7: 쿨다운 체크] 폭주 방지를 위한 최소 간격 보장 (KST 기준)
        var kstNow = DateTime.UtcNow.AddHours(9);
        if (_lastRecoveryAttempts.TryGetValue(chzzkUid, out var lastAttempt) &&
            kstNow - lastAttempt < RecoveryCooldown)
        {
            _logger.LogWarning("[피닉스의 제약] {ChzzkUid} 채널은 현재 복구 쿨다운 대기 중입니다. (폭주 방지)", chzzkUid);
            return;
        }

        // 2. [v5.0: 잠금 확보] 채널별 전용 잠금 확보 (중복 복구 시도 차단)
        var sema = _recoveryLocks.GetOrAdd(chzzkUid, _ => new SemaphoreSlim(1, 1));
        if (!await sema.WaitAsync(0)) return;

        try
        {
            // 3. [v5.7: 임계치 체크] 반복되는 실패 시 무한 루프를 포기하고 관리자 개입 요청
            int retryCount = _recoveryRetryCounts.GetOrAdd(chzzkUid, 0);
            if (retryCount >= MaxAutoRecoversPerWindow)
            {
                // [전략적 로그] LogCritical은 긴급 웹후크/모니터링 알림 트리거 규격입니다.
                _logger.LogCritical("🛑 [자가 치유 포기] {ChzzkUid} 채널이 연속 {MaxRetries}회 복구에 실패했습니다. 시스템 자원 보호를 위해 자동화 로직을 중단합니다. [재로그인 및 관리자 수동 확인 필요]", chzzkUid, MaxAutoRecoversPerWindow);
                return;
            }

            _logger.LogCritical("🚨 [오시리스의 자가 치유] {ChzzkUid} 채널 시스템 재구축 가동. (시도 {RetryCount}/{MaxRetries})", chzzkUid, retryCount + 1, MaxAutoRecoversPerWindow);
            _recoveryRetryCounts[chzzkUid] = retryCount + 1;

            // 1. [좀비 소멸]: 오염된 기존 수신 세션을 즉시 강제 종료
            await _chatClient.DisconnectAsync(chzzkUid);

            // 2. [영겁의 열쇠 연성]: 유효 기간에 상관없이 즉시 토큰 강제 갱신 시도
            // [v13.1] 주입된 _renewalService를 직접 사용하여 정합성 확보
            bool renewed = await _renewalService.RenewNowAsync(chzzkUid);

            if (renewed)
            {
                _logger.LogInformation("✨ [자가 치유 성공] {ChzzkUid} 채널의 토큰 파동이 정상화되었습니다. 2초 대기 후 피닉스의 재건을 가동합니다...", chzzkUid);
                
                // 성공 시 재시도 스택 초기화
                _recoveryRetryCounts.TryRemove(chzzkUid, out _);

                // [v6.0] 토큰 전파 시간 확보를 위한 미세 지연
                await Task.Delay(2000);
                
                // 3. [피닉스의 부활]: 가드 로직을 우회하여 핵심 연결 로직만 직접 호출 (데드락 방지)
                await ConnectInternalAsync(chzzkUid, forceFresh: true);
            }
            else
            {
                _logger.LogCritical("❌ [자가 치유 실패] {ChzzkUid} 채널의 토큰 갱신 프로토콜이 최종 실패했습니다. 리프레시 토큰 만료 또는 권한 오염이 의심됩니다! 수동 재로그인이 필요합니다.", chzzkUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 [자가 치유 치명적 오류] {ChzzkUid} 복구 프로세스 중 대폭발 발생", chzzkUid);
        }
        finally
        {
            // [v13.1] 최종 쿨다운 보정: 종료 시각을 KST(UTC+9)로 기록
            _lastRecoveryAttempts[chzzkUid] = DateTime.UtcNow.AddHours(9);
            sema.Release();
        }
    }

    public void CleanupRecoveryLock(string chzzkUid)
    {
        // [v5.7] 모든 추적 데이터 정화
        _lastRecoveryAttempts.TryRemove(chzzkUid, out _);
        _recoveryRetryCounts.TryRemove(chzzkUid, out _);

        if (_recoveryLocks.TryRemove(chzzkUid, out var sema))
        {
            _logger.LogDebug("[자가 치유 자원 해제] {ChzzkUid} 채널의 복구 전용 잠금을 해제했습니다.", chzzkUid);
            sema.Dispose();
        }
    }

    private void UpdateOrAddSystemSetting(string key, string value)
    {
        var setting = _db.SystemSettings.Local.FirstOrDefault(s => s.KeyName == key)
                   ?? _db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
        if (setting == null) _db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
        else setting.KeyValue = value;
    }
}
