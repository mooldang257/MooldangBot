using Microsoft.Extensions.Hosting;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Workers;

public class PeriodicMessageWorker : BackgroundService
{
    private readonly ILogger<PeriodicMessageWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOverlayNotificationService _overlayService;

    public PeriodicMessageWorker(ILogger<PeriodicMessageWorker> logger, IServiceProvider serviceProvider, IOverlayNotificationService overlayService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _overlayService = overlayService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [주기적 메시지 워커] 가동 중...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();

                // 1. 활성화된 모든 스트리머 프로필 조회 (N+1 방지 시작)
                var profiles = await db.StreamerProfiles
                    .AsNoTracking()
                    .Where(p => p.IsBotEnabled)
                    .ToListAsync(stoppingToken);

                if (profiles.Count > 0)
                {
                    var profileUids = profiles.Select(p => p.ChzzkUid).ToList();

                    // 2. 활성화된 모든 정기 메시지 일괄 조회 (N+1 해결)
                    var allMessages = await db.PeriodicMessages
                        .Where(m => profileUids.Contains(m.ChzzkUid) && m.IsEnabled)
                        .ToListAsync(stoppingToken);

                    var messagesLookup = allMessages.ToLookup(m => m.ChzzkUid);
                    var now = DateTimeOffset.UtcNow;

                    foreach (var profile in profiles)
                    {
                        var periodicMessages = messagesLookup[profile.ChzzkUid];

                        foreach (var msg in periodicMessages)
                        {
                            var lastSent = msg.LastSentAt != null 
                                ? new DateTimeOffset(msg.LastSentAt.Value, TimeSpan.Zero) 
                                : DateTimeOffset.MinValue;
                            
                            // 설정된 주기가 지났는지 확인 (타임존 독립적 비교)
                            if (now >= lastSent.AddMinutes(msg.IntervalMinutes))
                            {
                                _logger.LogInformation($"📢 [주기적 메시지] {profile.ChzzkUid} 채널 송출 시작: {msg.Message.Substring(0, Math.Min(msg.Message.Length, 20))}...");
                                
                                var success = await botService.SendReplyChatAsync(profile, msg.Message, "", stoppingToken);
                                
                                if (success)
                                {
                                    msg.LastSentAt = now.UtcDateTime;
                                    // 즉시 저장하여 워커 재시작 시 중복 발송 방지
                                    await db.SaveChangesAsync(stoppingToken);
                                    _logger.LogInformation($"✅ [주기적 메시지] {profile.ChzzkUid} 송출 완료 및 시간 갱신");
                                }
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
