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
        
        // 현재 전송해야 할 메세지들 조회
        var messages = await db.PeriodicMessages
            .Where(m => m.IsEnabled)
            .ToListAsync(stoppingToken);

        foreach (var m in messages)
        {
            // 발송 주기 체크
            if (m.LastSentAt.HasValue && now < m.LastSentAt.Value.AddMinutes(m.IntervalMinutes))
                continue;

            _logger.LogInformation($"🔍 [자동 메세지] {m.ChzzkUid} 채널 상태 확인 및 발송 시도...");

            // 방송 중인지 확인
            bool isLive = await _chzzkApiClient.IsLiveAsync(m.ChzzkUid);
            if (!isLive) 
            {
                _logger.LogDebug($"[자동 메세지] {m.ChzzkUid} 채널이 현재 방송 중이 아닙니다.");
                continue;
            }

            // 스트리머 프로필 및 토큰 정보 가져오기
            var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == m.ChzzkUid, stoppingToken);
            if (profile == null || string.IsNullOrEmpty(profile.ChzzkAccessToken)) continue;

            // 토큰 만료 임박 시 갱신
            await RefreshTokenIfNeededAsync(profile, db);

            // 채팅 전송
            bool success = await _chzzkApiClient.SendChatMessageAsync(profile.ChzzkAccessToken, m.Message);
            if (success)
            {
                m.LastSentAt = now;
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
