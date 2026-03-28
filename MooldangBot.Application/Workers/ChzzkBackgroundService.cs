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
        _logger.LogInformation("🚀 [치지직 백그라운드 서비스] 가동 중...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();
                var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();

                var profiles = await db.StreamerProfiles.Where(p => p.IsBotEnabled).ToListAsync(stoppingToken);

                foreach (var profile in profiles)
                {
                    // 1. [맥박의 점검]: 채팅 엔진(소켓) 연결 상태 보장 (24/7 유지)
                    await botService.EnsureConnectionAsync(profile.ChzzkUid);

                    // 2. [스마트 폴링 (P3)]: 최근 7일 내 방송 이력이 있거나, 최근 1시간 내 채팅 활동이 있거나, 이력이 아예 없는 경우 체크 (v2.3.3)
                    bool hasAnySession = await db.BroadcastSessions
                        .AnyAsync(s => s.ChzzkUid == profile.ChzzkUid, stoppingToken);

                    bool hasRecentSession = hasAnySession && await db.BroadcastSessions
                        .AnyAsync(s => s.ChzzkUid == profile.ChzzkUid && s.StartTime > DateTime.UtcNow.AddDays(-7), stoppingToken);
                    
                    bool isRecentlyChatted = scribe.IsRecentlyActive(profile.ChzzkUid);

                    // 기록이 아예 없는 신규 채널이거나, 최근 활동이 있는 경우에만 API 호출
                    if (!hasAnySession || hasRecentSession || isRecentlyChatted)
                    {
                        bool isLive = await _chzzkApi.IsLiveAsync(profile.ChzzkUid);
                        if (isLive)
                        {
                            _logger.LogInformation($"📡 [라이브 감지 성공] {profile.ChzzkUid} 채널이 현재 생방송 중입니다. 세션 하트비트를 자동 갱신합니다.");
                            await scribe.HeartbeatAsync(profile.ChzzkUid);
                        }
                        else
                        {
                            _logger.LogDebug($"[라이브 감지] {profile.ChzzkUid} 채널은 현재 오프라인 상태입니다.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [치지직 백그라운드 서비스] 실행 중 오류 발생");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
