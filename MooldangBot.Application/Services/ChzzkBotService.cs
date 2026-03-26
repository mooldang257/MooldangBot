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
    private readonly ILogger<ChzzkBotService> _logger;

    public ChzzkBotService(IAppDbContext db, IChzzkApiClient chzzkApi, ILogger<ChzzkBotService> logger)
    {
        _db = db;
        _chzzkApi = chzzkApi;
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

    public async Task<bool> SendReplyChatAsync(StreamerProfile profile, string message, CancellationToken token)
    {
        try
        {
            // ⭐ 계정 설정에 맞는 적절한 토큰 확보 (필요시 갱신 포함)
            string? tokenToUse = await GetBotTokenAsync(profile);
            if (string.IsNullOrEmpty(tokenToUse))
            {
                _logger.LogError($"❌ [ChzzkBotService] {profile.ChzzkUid} 유효한 발송 토큰을 찾을 수 없습니다.");
                return false;
            }

            _logger.LogInformation($"📡 [봇 채팅 발송] 대상채널: {profile.ChzzkUid}");

            return await _chzzkApi.SendChatMessageAsync(tokenToUse, profile.ChzzkUid, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [ChzzkBotService] {profile.ChzzkUid} 채팅 발송 에러: {ex.Message}");
            return false;
        }
    }

    public async Task RefreshChannelAsync(string chzzkUid)
    {
        _logger.LogInformation($"🔄 [봇 설정 갱신 요청] 채널: {chzzkUid}");
        // 실제 백그라운드 서비스의 연결을 재시도하거나 토큰을 즉시 갱신하는 로직이 들어갈 자리입니다.
        // 현재는 로그만 남기고, 향후 ChzzkChannelWorker 관리자와 연동될 것입니다.
        await Task.CompletedTask;
    }

    private void UpdateOrAddSystemSetting(string key, string value)
    {
        var setting = _db.SystemSettings.Local.FirstOrDefault(s => s.KeyName == key)
                   ?? _db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
        if (setting == null) _db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
        else setting.KeyValue = value;
    }
}
