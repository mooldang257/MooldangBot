using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data; // DB 컨텍스트
using System.Collections.Concurrent;
// ... 기타 필요한 using문
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;
using MooldangAPI.Models;
using SocketIOClient;
using System.Collections.Specialized;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MooldangAPI.Services;

// ASP.NET Core에서 계속 돌아가게 하려면 BackgroundService를 상속받는 것이 좋습니다.
public class BotManager : BackgroundService
{
    private readonly ILogger<BotManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private string _clientId;     // 캐싱용
    private string _clientSecret; // 캐싱용

    // ⭐ [교정 1] 멀티스레드 환경에서 절대 꼬이지 않는 ConcurrentDictionary 사용
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeChannels = new();

    public BotManager(ILogger<BotManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // 전체 봇 실행 시작점 (BackgroundService의 핵심)
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[물댕봇 매니저] 가동을 시작합니다...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // IConfiguration을 통해 appsettings.json 또는 환경 변수에서 치지직 API 키를 캐싱합니다.
            _clientId = config["ChzzkApi:ClientId"] ?? "";
            _clientSecret = config["ChzzkApi:ClientSecret"] ?? "";

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                _logger.LogError("[물댕봇] 설정 파일에서 치지직 API 키를 찾을 수 없습니다!");
                return;
            }
        }

        // 2. 무한 순찰 루프
        while (!stoppingToken.IsCancellationRequested)
        {
            await StartAllBotsAsync();
            await Task.Delay(60000, stoppingToken);
        }
    }

    public async Task StartAllBotsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
        {
            _logger.LogError("[물댕봇] DB에서 API 키를 찾을 수 없어 봇을 가동할 수 없습니다!");
            return;
        }

        // ⭐ 2. 이제 여기서는 스트리머 목록만 가볍게 가져옵니다. API 키 쿼리가 사라집니다!
        var streamerUids = await dbContext.StreamerProfiles.Select(p => p.ChzzkUid).ToListAsync();

        foreach (var uid in streamerUids)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                // 저장해둔 _clientId, _clientSecret를 건네줍니다.
                StartBotForStreamer(uid, _clientId, _clientSecret);
            }
        }

    }

    public void StartBotForStreamer(string uid, string clientId, string clientSecret)
    {
        if (_activeChannels.ContainsKey(uid)) return;

        var cts = new CancellationTokenSource();

        // 추가 시도 (성공하면 봇 실행)
        if (_activeChannels.TryAdd(uid, cts))
        {
            Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation($"[물댕봇] 채널 입장: {uid}");

                    // ⭐ 워커 생성 시 API 키를 함께 넘겨줍니다.
                    var worker = new ChzzkChannelWorker(uid, clientId, clientSecret, _serviceProvider);
                    await worker.ConnectAndListenAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[오류] {uid} 채널 처리 중 문제 발생: {ex.Message}");
                }
                finally
                {
                    _activeChannels.TryRemove(uid, out _);
                    _logger.LogWarning($"[물댕봇] 채널 퇴장 완료: {uid}");
                }
            }, cts.Token);
        }
    }

    public void StopBotForStreamer(string uid)
    {
        if (_activeChannels.TryRemove(uid, out var cts))
        {
            cts.Cancel(); // Worker 안의 웹소켓 ReceiveAsync 루프를 강제로 멈춥니다.
            _logger.LogInformation($"[물댕봇] 채널 수동 퇴장 명령: {uid}");
        }
    }
}