using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using System.Collections.Concurrent;

namespace MooldangAPI.Services;

public class ChzzkBackgroundService : BackgroundService
{
    private readonly ILogger<ChzzkBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private string _clientId = "";
    private string _clientSecret = "";

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeChannels = new();

    public ChzzkBackgroundService(ILogger<ChzzkBackgroundService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [Event-Driven] 치지직 멀티채널 매니저 가동을 시작합니다...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            _clientId = config["ChzzkApi:ClientId"] ?? "";
            _clientSecret = config["ChzzkApi:ClientSecret"] ?? "";

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                _logger.LogError("[물댕봇] 설정 파일에서 치지직 API 키를 찾을 수 없습니다!");
                return;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await StartAllBotsAsync();
            await Task.Delay(60000, stoppingToken);
        }
    }

    private async Task StartAllBotsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var streamerUids = await dbContext.StreamerProfiles.Select(p => p.ChzzkUid).ToListAsync();

        foreach (var uid in streamerUids)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                StartBotForStreamer(uid);
            }
        }
    }

    private void StartBotForStreamer(string uid)
    {
        if (_activeChannels.ContainsKey(uid)) return;

        var cts = new CancellationTokenSource();

        if (_activeChannels.TryAdd(uid, cts))
        {
            Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation($"[물댕봇] 채널 입장: {uid}");
                    var worker = new ChzzkChannelWorker(uid, _clientId, _clientSecret, _serviceProvider);
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
}
