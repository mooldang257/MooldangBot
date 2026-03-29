using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Services;

public class ChzzkBotService : IChzzkBotService
{
    private readonly IAppDbContext _db;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly IChzzkChatClient _chatClient;
    private readonly IDynamicQueryEngine _dynamicEngine; // [v1.9]
    private readonly ILogger<ChzzkBotService> _logger;

    public ChzzkBotService(
        IAppDbContext db, 
        IChzzkApiClient chzzkApi, 
        IChzzkChatClient chatClient, 
        IDynamicQueryEngine dynamicEngine, // [v1.9]
        ILogger<ChzzkBotService> logger)
    {
        _db = db;
        _chzzkApi = chzzkApi;
        _chatClient = chatClient;
        _dynamicEngine = dynamicEngine;
        _logger = logger;
    }

    public async Task<string?> GetBotTokenAsync(StreamerProfile profile)
    {
        // 🥇 1순위: 스트리머 커스텀 봇 계정 확인 및 갱신
        if (!string.IsNullOrEmpty(profile.BotRefreshToken))
        {
            if (!string.IsNullOrEmpty(profile.BotAccessToken) &&
                profile.BotTokenExpiresAt.HasValue &&
                profile.BotTokenExpiresAt.Value > DateTime.Now.AddHours(1))
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
                profile.BotTokenExpiresAt = DateTime.Now.AddSeconds(content.ExpiresIn);

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

        if (globalExpiresSetting != null && DateTime.TryParse(globalExpiresSetting.KeyValue, out var parsedDate))
        {
            globalExpireDate = parsedDate;
        }

        if (!string.IsNullOrEmpty(globalToken) && globalExpireDate > DateTime.Now.AddHours(1))
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
                DateTime newExpire = DateTime.Now.AddSeconds(content.ExpiresIn);

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
        // 1. 유효한 리프레시 토큰이 없으면 현재 액세스 토큰 반환 (폴백용)
        if (string.IsNullOrEmpty(profile.ChzzkRefreshToken)) return profile.ChzzkAccessToken;

        // 2. 토큰 만료 시간이 1시간 이상 남았다면 현재 토큰 그대로 사용
        if (!string.IsNullOrEmpty(profile.ChzzkAccessToken) &&
            profile.TokenExpiresAt.HasValue &&
            profile.TokenExpiresAt.Value > DateTime.Now.AddHours(1))
        {
            return profile.ChzzkAccessToken;
        }

        _logger.LogWarning($"🔄 [피닉스] {profile.ChzzkUid}님의 스트리머 토큰 만료 임박! 자동 갱신 시도...");

        // 3. 리프레시 토큰을 사용하여 새로운 액세스 토큰 획득
        var tokenRes = await _chzzkApi.RefreshTokenAsync(profile.ChzzkRefreshToken);
        if (tokenRes != null && tokenRes.Code == 200 && tokenRes.Content != null)
        {
            var content = tokenRes.Content;
            profile.ChzzkAccessToken = content.AccessToken;
            profile.ChzzkRefreshToken = content.RefreshToken ?? profile.ChzzkRefreshToken;
            profile.TokenExpiresAt = DateTime.Now.AddSeconds(content.ExpiresIn);

            // 💾 [자가 저장]: 갱신된 토큰 정보를 즉시 DB에 반영
            await _db.SaveChangesAsync();

            _logger.LogInformation($"✅ [피닉스] {profile.ChzzkUid}님의 스트리머 토큰 갱신 성공!");
            return profile.ChzzkAccessToken;
        }

        _logger.LogError($"❌ [피닉스] {profile.ChzzkUid}님의 스트리머 토큰 갱신 실패! 기존 토큰을 시도합니다.");
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

    public async Task EnsureConnectionAsync(string chzzkUid)
    {
        // 1. [상태 확인]: 이미 연결되어 있다면 통과
        if (_chatClient.IsConnected(chzzkUid))
        {
            _logger.LogDebug($"[피닉스 점검] {chzzkUid} 세션이 이미 안정적으로 유지 중입니다.");
            return;
        }

        _logger.LogWarning($"[피닉스의 재건] {chzzkUid} 세션 단절 감지. 정화를 시작합니다.");

        try
        {
            // 2. [최신 토큰 조회]: 와치독이 갱신했을 수 있는 최신 토큰을 DB에서 조회 (추적 활성화)
            var profile = await _db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null || !profile.IsBotEnabled)
            {
                _logger.LogWarning($"[피닉스 중단] {chzzkUid} 봇이 존재하지 않거나 비활성화되었습니다.");
                return;
            }

            // 3. [토큰 상태 확인 및 갱신]: 401 인증 오류 방지를 위해 연결 전 토큰 체크
            string? validToken = await GetStreamerTokenAsync(profile);
            
            if (string.IsNullOrEmpty(validToken))
            {
                _logger.LogWarning($"[피닉스 중단] {chzzkUid} 채널의 유효한 인증 토큰을 확보하지 못했습니다.");
                return;
            }

            // 4. [유기적 복구]: 기존 좀비 자원 정리 후 재연결
            await _chatClient.DisconnectAsync(chzzkUid);
            
            bool success = await _chatClient.ConnectAsync(chzzkUid, validToken);

            if (success)
                _logger.LogInformation($"✅ [피닉스 부활] {chzzkUid} 세션이 성공적으로 재건되었습니다.");
            else
                _logger.LogError($"❌ [피닉스 실패] {chzzkUid} 세션 재건에 실패했습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [피닉스 예외] {chzzkUid} 복구 중 예기치 못한 오류 발생");
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
