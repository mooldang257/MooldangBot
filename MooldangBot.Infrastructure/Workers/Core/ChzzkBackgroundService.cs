using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Models.Chzzk;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;

namespace MooldangBot.Infrastructure.Workers.Core;

public class ChzzkBackgroundService(
    ILogger<ChzzkBackgroundService> logger, 
    IServiceScopeFactory scopeFactory,
    IChzzkChatClient chatClient,
    IOptionsMonitor<WorkerSettings> optionsMonitor) : BackgroundService
{
    private const string WorkerName = nameof(ChzzkBackgroundService);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 獲得
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [ChzzkBackgroundService] 가동 시작 (설정: {Interval}s)", CurrentSettings.IntervalSeconds);

        try
        {
            logger.LogInformation("[파동의 시동] 채팅 클라이언트 초기화를 시작합니다...");
            await chatClient.InitializeAsync();
            logger.LogInformation("✅ [파동의 시동] 채팅 클라이언트가 성공적으로 초기화되었습니다.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "❌ [파동의 침몰] 채팅 클라이언트 초기화 중 치명적 오류 발생. 서비스를 중단합니다.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = CurrentSettings;
            if (!settings.IsEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            // [N7 해결]: 이전 폴링 작업이 진행 중이면 이번 주기를 건너뜁니다.
            if (!await _semaphore.WaitAsync(0, stoppingToken))
            {
                logger.LogWarning("⚠️ [ChzzkBackgroundService] 이전 점검 작업이 아직 완료되지 않았습니다. 이번 주기를 건너뜜.");
            }
            else
            {
                try
                {
                    List<string> activeUids;

                    // 1단계: 활성 스트리머 UID 목록 조회
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                        activeUids = await db.StreamerProfiles
                            .Where(p => p.IsActive && p.IsMasterEnabled)
                            .Select(p => p.ChzzkUid)
                            .ToListAsync(stoppingToken);
                    }

                    if (activeUids.Count > 0)
                    {
                        logger.LogInformation("📊 [병렬 배치] 활성 스트리머 {Count}명 점검을 시작합니다.", activeUids.Count);

                        // 2단계: 병렬 배치 처리
                        await Parallel.ForEachAsync(activeUids,
                            new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = stoppingToken },
                            async (chzzkUid, ct) =>
                            {
                                try
                                {
                                    using var scope = scopeFactory.CreateScope();
                                    var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
                                    var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();
                                    var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
                                    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                                    await botService.EnsureConnectionAsync(chzzkUid);

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
                                            logger.LogInformation("📡 [라이브 감지] {ChzzkUid} 채널 방송 중 확인.", chzzkUid);
                                            await scribe.HeartbeatAsync(chzzkUid);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, "⚠️ {ChzzkUid} 채널 점검 중 개별 오류 (격리 처리)", chzzkUid);
                                }
                            });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ [ChzzkBackgroundService] 전역 오류 발생");
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
        }
    }
}
