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

        // IsBotEnabled가 true인 스트리머만 가져온다.
        var enabledUids = await dbContext.StreamerProfiles
            .Where(p => p.IsBotEnabled)
            .Select(p => p.ChzzkUid)
            .ToListAsync();

        var enabledUidSet = new HashSet<string>(enabledUids.Where(u => !string.IsNullOrEmpty(u))!);

        // 1. 활성화된 스트리머 봇 접속
        foreach (var uid in enabledUidSet)
        {
            StartBotForStreamer(uid);
        }

        // 2. 비활성화된 스트리머 봇 접속 해제
        var currentActiveUids = _activeChannels.Keys.ToList();
        foreach (var activeUid in currentActiveUids)
        {
            if (!enabledUidSet.Contains(activeUid))
            {
                StopBotForStreamer(activeUid);
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

    private void StopBotForStreamer(string uid)
    {
        if (_activeChannels.TryGetValue(uid, out var cts))
        {
            _logger.LogInformation($"[물댕봇] 채널 강제 퇴장 요청 (봇 비활성화): {uid}");
            cts.Cancel(); 
            // 취소 요청 시 worker.ConnectAndListenAsync 내부의 Task가 취소되며 finally 블록에서 TryRemove 됩니다.
        }
    }
}
