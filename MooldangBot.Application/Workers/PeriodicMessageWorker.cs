using Microsoft.Extensions.Hosting;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Workers;

public class PeriodicMessageWorker : BackgroundService
{
    private readonly ILogger<PeriodicMessageWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PeriodicMessageWorker(ILogger<PeriodicMessageWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [주기적 메시지 워커] 가동 중...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();
                var overlayService = scope.ServiceProvider.GetRequiredService<IOverlayNotificationService>();

                // 1. 활성화된 모든 스트리머 프로필 조회 (N+1 방지 시작)
                var profiles = await db.StreamerProfiles
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.IsMasterEnabled) // [v6.1.6] 활동성 및 마스터 킬 스위치 통합 점검
                    .ToListAsync(stoppingToken);

                if (profiles.Count > 0)
                {
                    var profileIds = profiles.Select(p => p.Id).ToList();

                    // 2. 활성화된 모든 정기 메시지 일괄 조회 (N+1 해결)
                    var allMessages = await db.PeriodicMessages
                        .Where(m => profileIds.Contains(m.StreamerProfileId) && m.IsEnabled)
                        .ToListAsync(stoppingToken);

                    var messagesLookup = allMessages.ToLookup(m => m.StreamerProfileId);
                    var now = KstClock.Now;

                    foreach (var profile in profiles)
                    {
                        var periodicMessages = messagesLookup[profile.Id];

                        foreach (var msg in periodicMessages)
                        {
                            var lastSent = msg.LastSentAt ?? KstClock.MinValue;
                            
                            // 설정된 주기가 지났는지 확인 (타임존 독립적 비교)
                            if (now >= lastSent.AddMinutes(msg.IntervalMinutes))
                            {
                                // 메시지 송출 명령 발행 (Fire & Forget)
                                await botService.SendReplyChatAsync(profile, msg.Message, "", stoppingToken);
                                
                                msg.LastSentAt = now;
                                // 즉시 저장하여 워커 재시작 시 중복 발송 방지
                                await db.SaveChangesAsync(stoppingToken);
                                _logger.LogInformation($"✅ [주기적 메시지] {profile.ChzzkUid} 송출 완료 및 시간 갱신");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [주기적 메시지 워커] 실행 중 오류 발생");
            }

            // 1분 단위로 체크
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
