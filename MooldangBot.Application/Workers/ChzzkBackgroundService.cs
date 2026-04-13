using Microsoft.Extensions.Hosting;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Contracts.Models.Chzzk;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Workers;

public class ChzzkBackgroundService : BackgroundService
{
    private readonly ILogger<ChzzkBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChzzkChatClient _chatClient;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ChzzkBackgroundService(
        ILogger<ChzzkBackgroundService> logger, 
        IServiceScopeFactory scopeFactory,
        IChzzkChatClient chatClient)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [치지직 백그라운드 서비스] 가동 중... (Phase1: 병렬 배치 처리 모드)");

        try
        {
            _logger.LogInformation("[파동의 시동] 채팅 클라이언트 초기화를 시작합니다...");
            await _chatClient.InitializeAsync();
            _logger.LogInformation("✅ [파동의 시동] 채팅 클라이언트가 성공적으로 초기화되었습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "❌ [파동의 침몰] 채팅 클라이언트 초기화 중 치명적 오류 발생. 서비스를 중단합니다.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            // [N7 해결]: 이전 폴링 작업이 진행 중이면 이번 주기를 건너뜁니다.
            if (!await _semaphore.WaitAsync(0, stoppingToken))
            {
                _logger.LogWarning("⚠️ [치지직 백그라운드 서비스] 이전 점검 작업이 아직 완료되지 않았습니다. 이번 주기를 건너뜁니다.");
            }
            else
            {
                try
                {
                    List<string> activeUids;

                    // 1단계: 활성 스트리머 UID 목록 조회 (작은 스코프에서 DB 작업 수행)
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                        activeUids = await db.StreamerProfiles
                            .Where(p => p.IsActive && p.IsMasterEnabled) // [v6.1.6] 마스터 스위치 및 활동성 체크로 전환
                            .Select(p => p.ChzzkUid)
                            .ToListAsync(stoppingToken);
                    }

                    _logger.LogInformation($"📊 [병렬 배치] 활성 스트리머 {activeUids.Count}명 점검을 시작합니다. (MaxParallelism=10)");

                    // 2단계: 병렬 배치 처리 (채널당 독립 Scope 활용)
                    await Parallel.ForEachAsync(activeUids,
                        new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = stoppingToken },
                        async (chzzkUid, ct) =>
                        {
                            try
                            {
                                // [시니어 팁]: 각 병렬 작업마다 독립적인 스코프를 생성하여 HttpClient/DB 객체 수명 주기를 보장합니다.
                                using var scope = _scopeFactory.CreateScope();
                                var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
                                var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();
                                var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
                                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                                // [맥박의 점검]: 채팅 엔진(소켓) 연결 상태 보장
                                await botService.EnsureConnectionAsync(chzzkUid);

                                // [스마트 폴링]: 방송 이력/활동 여부에 따른 지능형 조회
                                bool hasAnySession = await db.BroadcastSessions
                                    .AnyAsync(s => s.StreamerProfile!.ChzzkUid == chzzkUid, ct);

                                bool hasRecentSession = hasAnySession && await db.BroadcastSessions
                                    .AnyAsync(s => s.StreamerProfile!.ChzzkUid == chzzkUid && s.StartTime > KstClock.Now.AddDays(-7), ct);


                                bool isRecentlyChatted = scribe.IsRecentlyActive(chzzkUid);

                                if (!hasAnySession || hasRecentSession || isRecentlyChatted)
                                {
                                    // [v10.1] IChzzkApiClient를 통한 라이브 상태 확인
                                    var liveResult = await chzzkApi.GetLiveDetailAsync(chzzkUid);
                                    bool isLiveNow = liveResult?.Content?.Status == "OPEN";
                                    if (isLiveNow)
                                    {
                                        _logger.LogInformation($"📡 [라이브 감지 성공] {chzzkUid} 채널 방송 중.");
                                        await scribe.HeartbeatAsync(chzzkUid);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"⚠️ {chzzkUid} 채널 점검 중 개별 오류 (격리 처리)");
                            }
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [치지직 백그라운드 서비스] 전역 오류 발생");
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
