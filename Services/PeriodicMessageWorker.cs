using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
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

            // 라이브 상태 확인 (캐시 사용)
            if (!liveStatusCache.TryGetValue(m.ChzzkUid, out bool isLive))
            {
                // 스트리머 프로필에서 봇 활성화 여부 먼저 확인
                var profileCheck = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == m.ChzzkUid, stoppingToken);
                if (profileCheck == null || !profileCheck.IsBotEnabled)
                {
                    liveStatusCache[m.ChzzkUid] = false; // 봇이 비활성화면 라이브 여부와 상관없이 발송 안 함
                    _logger.LogDebug($"[자동 메세지] {m.ChzzkUid} 채널의 물댕봇이 비활성화 상태입니다.");
                    continue;
                }

                isLive = await _chzzkApiClient.IsLiveAsync(m.ChzzkUid);
                liveStatusCache[m.ChzzkUid] = isLive;
                _logger.LogInformation($"[자동 메세지] {m.ChzzkUid} 라이브 상태: {isLive}");
            }

            if (!isLive) 
            {
                _logger.LogDebug($"[자동 메세지] {m.ChzzkUid} 채널이 현재 방송 중이 아닙니다.");
                continue;
            }

            // 스트리머 프로필 및 토큰 정보 가져오기
            var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == m.ChzzkUid, stoppingToken);
            if (profile == null || string.IsNullOrEmpty(profile.ChzzkAccessToken)) 
            {
                _logger.LogWarning($"[자동 메세지] {m.ChzzkUid}의 프로필 또는 토큰이 없습니다.");
                continue;
            }

            // 토큰 만료 임박 시 갱신
            var trackedProfile = await db.StreamerProfiles.FindAsync(new object[] { profile.Id }, stoppingToken);
            string currentToken = profile.ChzzkAccessToken;
            
            if (trackedProfile != null)
            {
                await RefreshTokenIfNeededAsync(trackedProfile, db);
                currentToken = trackedProfile.ChzzkAccessToken;
            }

            // 채팅 전송 (최신 토큰 사용)
            bool success = await _chzzkApiClient.SendChatMessageAsync(currentToken, m.Message);
            if (success)
            {
                // 엔티티를 다시 가져와서 상태 업데이트 (교차 방지용 추적 객체 사용)
                var trackedMsg = await db.PeriodicMessages.FindAsync(new object[] { m.Id }, stoppingToken);
                if (trackedMsg != null)
                {
                    trackedMsg.LastSentAt = now;
                }
                _logger.LogInformation($"✅ [자동 메세지] {m.ChzzkUid} 채널에 메세지 발송 완료: {m.Message}");
            }
            else
            {
                _logger.LogWarning($"❌ [자동 메세지] {m.ChzzkUid} 채널 메세지 발송 실패");
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private async Task RefreshTokenIfNeededAsync(StreamerProfile profile, AppDbContext db)
    {
        if (profile.TokenExpiresAt.HasValue && profile.TokenExpiresAt.Value > DateTime.Now.AddHours(1))
            return;

        _logger.LogWarning($"🔄 [자동 메세지] {profile.ChzzkUid}의 액세스 토큰 갱신 시도...");

        using var httpClient = new HttpClient();
        var tokenRequest = new 
        { 
            grantType = "refresh_token", 
            clientId = _clientId, 
            clientSecret = _clientSecret, 
            refreshToken = profile.ChzzkRefreshToken 
        };
        
        var content = new StringContent(JsonSerializer.Serialize(tokenRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://openapi.chzzk.naver.com/auth/v1/token", content);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var tokenContent = doc.RootElement.GetProperty("content");

            profile.ChzzkAccessToken = tokenContent.GetProperty("accessToken").GetString() ?? "";
            profile.ChzzkRefreshToken = tokenContent.GetProperty("refreshToken").GetString() ?? profile.ChzzkRefreshToken;
            profile.TokenExpiresAt = DateTime.Now.AddSeconds(tokenContent.GetProperty("expiresIn").GetInt32());

            await db.SaveChangesAsync();
            _logger.LogInformation($"✅ [자동 메세지] {profile.ChzzkUid} 토큰 갱신 완료");
        }
        else
        {
            _logger.LogError($"❌ [자동 메세지] {profile.ChzzkUid} 토큰 갱신 실패");
        }
    }
}
