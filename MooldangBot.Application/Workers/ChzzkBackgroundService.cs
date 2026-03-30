using Microsoft.Extensions.Hosting;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Workers;

public class ChzzkBackgroundService : BackgroundService
{
    private readonly ILogger<ChzzkBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IChzzkApiClient _chzzkApi;

    public ChzzkBackgroundService(ILogger<ChzzkBackgroundService> logger, IServiceProvider serviceProvider, IChzzkApiClient chzzkApi)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _chzzkApi = chzzkApi;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [치지직 백그라운드 서비스] 가동 중... (Phase1: 병렬 배치 처리 모드)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<string> activeUids;

                // 1단계: 활성 스트리머 UID 목록 조회 (짧은 Scope)
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    activeUids = await db.StreamerProfiles
                        .Where(p => p.IsBotEnabled)
                        .Select(p => p.ChzzkUid)
                        .ToListAsync(stoppingToken);
                }

                _logger.LogInformation($"📊 [병렬 배치] 활성 스트리머 {activeUids.Count}명에 대한 병렬 점검을 시작합니다. (MaxParallelism=10)");

                // 2단계: 병렬 배치 처리 (채널당 독립 Scope)
                await Parallel.ForEachAsync(activeUids,
                    new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = stoppingToken },
                    async (chzzkUid, ct) =>
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();
                            var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
                            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                            // [맥박의 점검]: 채팅 엔진(소켓) 연결 상태 보장 (24/7 유지)
                            await botService.EnsureConnectionAsync(chzzkUid);

                            // [스마트 폴링 (P3)]: 최근 7일 내 방송 이력이 있거나, 최근 1시간 내 채팅 활동이 있거나, 이력이 아예 없는 경우 체크
                            bool hasAnySession = await db.BroadcastSessions
                                .AnyAsync(s => s.ChzzkUid == chzzkUid, ct);

                            bool hasRecentSession = hasAnySession && await db.BroadcastSessions
                                .AnyAsync(s => s.ChzzkUid == chzzkUid && s.StartTime > DateTime.UtcNow.AddDays(-7), ct);

                            bool isRecentlyChatted = scribe.IsRecentlyActive(chzzkUid);

                            // 기록이 아예 없는 신규 채널이거나, 최근 활동이 있는 경우에만 API 호출
                            if (!hasAnySession || hasRecentSession || isRecentlyChatted)
                            {
                                bool isLive = await _chzzkApi.IsLiveAsync(chzzkUid);
                                if (isLive)
                                {
                                    _logger.LogInformation($"📡 [라이브 감지 성공] {chzzkUid} 채널이 현재 생방송 중입니다.");
                                    await scribe.HeartbeatAsync(chzzkUid);
                                }
                                else
                                {
                                    _logger.LogDebug($"[라이브 감지] {chzzkUid} 채널은 현재 오프라인 상태입니다.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"⚠️ [병렬 배치] {chzzkUid} 채널 점검 중 개별 오류 발생 (다른 채널에 영향 없음)");
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [치지직 백그라운드 서비스] 실행 중 오류 발생");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
