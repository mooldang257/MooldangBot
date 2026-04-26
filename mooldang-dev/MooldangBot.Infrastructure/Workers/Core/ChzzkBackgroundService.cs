using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;

namespace MooldangBot.Infrastructure.Workers.Core;

/// <summary>
/// [치지직 워커]: 활성 스트리머들의 라이브 상태를 주기적으로 점검하고 채팅 연결을 확보합니다.
/// </summary>
public class ChzzkBackgroundService(IServiceProvider serviceProvider,
    
    ILogger<ChzzkBackgroundService> logger, 
    IServiceScopeFactory scopeFactory,
    IChzzkChatClient chatClient,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(ChzzkBackgroundService))
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // [지휘관 지침]: 치지직 상태 점검 주기는 기본 60초로 설정합니다.
    protected override int DefaultIntervalSeconds => 60;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[파동의 시동] 채팅 클라이언트 초기화를 시작합니다...");
            await chatClient.InitializeAsync();
            _logger.LogInformation("✅ [파동의 시동] 채팅 클라이언트 초기화 완료.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "❌ [파동의 침몰] 초기화 실패. 서비스를 중단합니다.");
            throw; // 초기화 실패 시 어플리케이션 기동 중단 (기존 로직 유지)
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        if (!await _semaphore.WaitAsync(0, ct))
        {
            _logger.LogWarning("⚠️ [ChzzkBackgroundService] 이전 점검 작업이 미완료 상태입니다. 건너뜁니다.");
            return;
        }

        try
        {
            List<string> activeUids;
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                // [물멍]: 감시 워커는 활성 상태이며 마스터 승인이 된 스트리머만 추적합니다.
                activeUids = await db.StreamerProfiles
                    .Where(p => p.IsActive && p.IsMasterEnabled)
                    .Select(p => p.ChzzkUid)
                    .ToListAsync(ct);
            }

            if (activeUids.Count > 0)
            {
                _logger.LogInformation("📊 [병렬 배치] 활성 스트리머 {Count}명 라이브 여부를 점검합니다.", activeUids.Count);

                await Parallel.ForEachAsync(activeUids,
                    new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct },
                    async (chzzkUid, token) =>
                    {
                        await CheckLiveStatusAsync(chzzkUid, token);
                    });
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CheckLiveStatusAsync(string chzzkUid, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();
            var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            // 채팅 연결 확보 (필요 시 자동 연결 및 비활성 시 해제)
            await botService.EnsureConnectionAsync(chzzkUid);

            // 세션 기록이 없거나, 최근 기록이 있거나, 봇이 최근 채팅을 받았을 경우 API 호출 (부하 분산)
            bool hasAnySession = await db.BroadcastSessions
                .AnyAsync(s => s.StreamerProfile!.ChzzkUid == chzzkUid, ct);
            bool hasRecentSession = hasAnySession && await db.BroadcastSessions
                .AnyAsync(s => s.StreamerProfile!.ChzzkUid == chzzkUid && s.StartTime > KstClock.Now.AddDays(-7), ct);
            
            bool isRecentlyActive = scribe.IsRecentlyActive(chzzkUid);

            if (!hasAnySession || hasRecentSession || isRecentlyActive)
            {
                var liveResult = await chzzkApi.GetLiveDetailAsync(chzzkUid);
                if (liveResult?.Status == "OPEN")
                {
                    await scribe.HeartbeatAsync(chzzkUid);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ {ChzzkUid} 개별 점검 장애: {Msg}", chzzkUid, ex.Message);
        }
    }
}
