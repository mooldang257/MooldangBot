using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using System.Collections.Concurrent;
using MooldangAPI.ApiClients;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;
 
namespace MooldangAPI.Services;
 
public class ChzzkBackgroundService : BackgroundService
{
    private readonly ILogger<ChzzkBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ChzzkApiClient _chzzkApiClient;
    private readonly IHubContext<OverlayHub> _hubContext;
    private string _clientId = "";
    private string _clientSecret = "";
 
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeChannels = new();
    // 라이브 상태 추적용 (이전 상태와 비교하여 롤백 트리거)
    private readonly ConcurrentDictionary<string, bool> _lastLiveState = new();
 
    public ChzzkBackgroundService(ILogger<ChzzkBackgroundService> logger, IServiceProvider serviceProvider, ChzzkApiClient chzzkApiClient, IHubContext<OverlayHub> hubContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _chzzkApiClient = chzzkApiClient;
        _hubContext = hubContext;
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
            await CheckLiveStatusAndRollbackAsync(stoppingToken);
            await Task.Delay(60000, stoppingToken);
        }
    }

    // 특정 채널의 봇 상태를 즉시 갱신 (활성화 시 시작, 비활성화 시 종료)
    public async Task RefreshChannelAsync(string uid)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await dbContext.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == uid);

        if (profile == null || !profile.IsBotEnabled)
        {
            StopBotForStreamer(uid);
        }
        else
        {
            StartBotForStreamer(uid);
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

    private async Task CheckLiveStatusAndRollbackAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profiles = await dbContext.StreamerProfiles
            .Where(p => p.IsBotEnabled && !string.IsNullOrEmpty(p.ChzzkUid))
            .ToListAsync(stoppingToken);

        // ⭐ [성능 개선 #3] 스트리머별 라이브 상태 API 호출을 순차 → Task.WhenAll 병력로 변경
        var tasks = profiles.Select(async profile =>
        {
            try
            {
                bool isLive = await _chzzkApiClient.IsLiveAsync(profile.ChzzkUid!, profile.ChzzkAccessToken);
                bool wasLive = _lastLiveState.TryGetValue(profile.ChzzkUid!, out bool last) && last;

                // Live -> Offline 전환 감지
                if (wasLive && !isLive)
                {
                    _logger.LogInformation($"[Rollback] Streamer {profile.ChzzkUid} went offline. Rolling back preset...");

                    // ⭐ 롤백 작업은 별도 scope에서 DB 사용
                    using var rollbackScope = _serviceProvider.CreateScope();
                    var rollbackDb = rollbackScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var hubContext = rollbackScope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<MooldangAPI.Hubs.OverlayHub>>();

                    var defaultPreset = await rollbackDb.OverlayPresets
                        .Where(p => p.ChzzkUid == profile.ChzzkUid)
                        .OrderBy(p => p.Id)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (defaultPreset != null)
                    {
                        var profileToUpdate = await rollbackDb.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == profile.ChzzkUid, stoppingToken);
                        if (profileToUpdate != null)
                        {
                            profileToUpdate.ActiveOverlayPresetId = defaultPreset.Id;
                            await rollbackDb.SaveChangesAsync(stoppingToken);
                        }

                        await hubContext.Clients.Group(profile.ChzzkUid!).SendAsync("ReceiveOverlayStyle", defaultPreset.ConfigJson, stoppingToken);
                        _logger.LogInformation($"[Rollback] Preset rolled back to {defaultPreset.Name} for {profile.ChzzkUid}");
                    }
                }

                _lastLiveState[profile.ChzzkUid!] = isLive;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Rollback Error] Failed to check status for {profile.ChzzkUid}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
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
