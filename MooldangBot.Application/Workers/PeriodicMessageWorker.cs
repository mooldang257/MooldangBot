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
                var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
                var botService = scope.ServiceProvider.GetRequiredService<IChzzkBotService>();

                var profiles = await db.StreamerProfiles.Where(p => p.IsBotEnabled).ToListAsync(stoppingToken);
                var now = DateTime.Now;

                foreach (var profile in profiles)
                {
                    var periodicMessages = await db.PeriodicMessages
                        .Where(m => m.ChzzkUid == profile.ChzzkUid && m.IsEnabled)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in periodicMessages)
                    {
                        var lastSent = msg.LastSentAt ?? DateTime.MinValue;
                        
                        // 설정된 주기가 지났는지 확인
                        if (now >= lastSent.AddMinutes(msg.IntervalMinutes))
                        {
                            _logger.LogInformation($"📢 [주기적 메시지] {profile.ChzzkUid} 채널 송출 시작: {msg.Message.Substring(0, Math.Min(msg.Message.Length, 20))}...");
                            
                            // 정기 메시지는 특정 시청자 대응이 아니므로 viewerUid를 빈값으로 전송
                            var success = await botService.SendReplyChatAsync(profile, msg.Message, "", stoppingToken);
                            
                            if (success)
                            {
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

            // 1분 단위로 체크하여 정밀도 향상
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
