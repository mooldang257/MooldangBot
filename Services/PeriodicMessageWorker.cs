using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangAPI.ApiClients;
using System.Text.Json;
using System.Text;

namespace MooldangAPI.Services;

public class PeriodicMessageWorker : BackgroundService
{
    private readonly ILogger<PeriodicMessageWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ChzzkApiClient _chzzkApiClient;
    private string _clientId = "";
    private string _clientSecret = "";

    public PeriodicMessageWorker(ILogger<PeriodicMessageWorker> logger, IServiceProvider serviceProvider, ChzzkApiClient chzzkApiClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _chzzkApiClient = chzzkApiClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📢 [자동 메세지 배치] 워커 가동을 시작합니다...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            _clientId = config["ChzzkApi:ClientId"] ?? "";
            _clientSecret = config["ChzzkApi:ClientSecret"] ?? "";
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPeriodicMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [자동 메세지 배치] 오류 발생: {ex.Message}");
            }

            // 1분마다 체크 (IntervalMinutes가 분 단위이므로)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessPeriodicMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.Now;
        
        // 현재 활성화된 모든 정기 메세지 가져오기
        var messages = await db.PeriodicMessages
            .Where(m => m.IsEnabled)
            .ToListAsync(stoppingToken);

        // 스트리머별 라이브 상태 캐싱 (중복 호출 방지)
        var liveStatusCache = new Dictionary<string, bool>();

        foreach (var m in messages)
        {
            // 발송 주기 체크
            if (m.LastSentAt.HasValue && now < m.LastSentAt.Value.AddMinutes(m.IntervalMinutes))
                continue;

            // ⭐ [성능 개선 #2] 동일 스트리머 프로파일을 3번 조회 → 1번으로 통합
            // 라이브 상태 캨시가 없으면 DB에서 한 번 조회용
            if (!liveStatusCache.ContainsKey(m.ChzzkUid))
            {
                // 프로파일 + 보트 활성화 여부를 한 번에 조회
                var profile = await db.StreamerProfiles
                    .FirstOrDefaultAsync(p => p.ChzzkUid == m.ChzzkUid, stoppingToken);

                if (profile == null || !profile.IsBotEnabled)
                {
                    liveStatusCache[m.ChzzkUid] = false;
                    _logger.LogDebug($"[자동 메세지] {m.ChzzkUid} 채널의 물댓보이 비활성화 상태입니다.");
                    continue;
                }

                bool isLive = await _chzzkApiClient.IsLiveAsync(m.ChzzkUid, profile.ChzzkAccessToken);
                liveStatusCache[m.ChzzkUid] = isLive;
                _logger.LogInformation($"[자동 메세지] {m.ChzzkUid} 라이브 상태: {isLive}");

                if (!isLive)
                {
                    _logger.LogDebug($"[자동 메세지] {m.ChzzkUid} 채널이 현재 방송 중이 아닙니다.");
                    continue;
                }

                // 토큰 만료 임박 시 갱신 (AsTracking 상태)
                await RefreshTokenIfNeededAsync(profile, db);

                string? currentToken = profile.ChzzkAccessToken;
                if (string.IsNullOrEmpty(currentToken))
                {
                    _logger.LogWarning($"[자동 메세지] {m.ChzzkUid}의 토큰을 가져올 수 없습니다.");
                    continue;
                }

                // 채팅 전송 (최신 토큰 사용)
                bool success = await _chzzkApiClient.SendChatMessageAsync(currentToken, m.Message);
                if (success)
                {
                    var trackedMsg = await db.PeriodicMessages.FindAsync(new object[] { m.Id }, stoppingToken);
                    if (trackedMsg != null) trackedMsg.LastSentAt = now;
                    _logger.LogInformation($"✅ [자동 메세지] {m.ChzzkUid} 채널에 메세지 발송 완료: {m.Message}");
                }
                else
                {
                    _logger.LogWarning($"❌ [자동 메세지] {m.ChzzkUid} 채널 메세지 발송 실패");
                }
                continue;
            }

            // 기존 캐시에 라이브 상태가 있는 스트리머
            if (!liveStatusCache[m.ChzzkUid])
            {
                _logger.LogDebug($"[자동 메세지] {m.ChzzkUid} 채널이 현재 방송 중이 아닙니다.");
                continue;
            }

            var cachedProfile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == m.ChzzkUid, stoppingToken);
            if (cachedProfile == null || string.IsNullOrEmpty(cachedProfile.ChzzkAccessToken)) continue;

            await RefreshTokenIfNeededAsync(cachedProfile, db);

            bool sent = await _chzzkApiClient.SendChatMessageAsync(cachedProfile.ChzzkAccessToken!, m.Message);
            if (sent)
            {
                var trackedMsg = await db.PeriodicMessages.FindAsync(new object[] { m.Id }, stoppingToken);
                if (trackedMsg != null) trackedMsg.LastSentAt = now;
                _logger.LogInformation($"✅ [자동 메세지] {m.ChzzkUid} 채널에 메세지 발송 완료: {m.Message}");
            }
            else
                _logger.LogWarning($"❌ [자동 메세지] {m.ChzzkUid} 채널 메세지 발송 실패");
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private async Task RefreshTokenIfNeededAsync(StreamerProfile profile, AppDbContext db)
    {
        if (profile.TokenExpiresAt.HasValue && profile.TokenExpiresAt.Value > DateTime.Now.AddHours(1))
            return;

        _logger.LogWarning($"🔄 [자동 메세지] {profile.ChzzkUid}의 액세스 토큰 갱신 시도...");

        var tokenResponse = await _chzzkApiClient.RefreshTokenAsync(profile.ChzzkRefreshToken ?? "");

        if (tokenResponse != null && tokenResponse.Code == 200 && tokenResponse.Content != null)
        {
            var content = tokenResponse.Content;
            profile.ChzzkAccessToken = content.AccessToken ?? "";
            profile.ChzzkRefreshToken = content.RefreshToken ?? profile.ChzzkRefreshToken;
            profile.TokenExpiresAt = DateTime.Now.AddSeconds(content.ExpiresIn);

            await db.SaveChangesAsync();
            _logger.LogInformation($"✅ [자동 메세지] {profile.ChzzkUid} 토큰 갱신 완료");
        }
        else
        {
            _logger.LogError($"❌ [자동 메세지] {profile.ChzzkUid} 토큰 갱신 실패");
        }
    }
}
