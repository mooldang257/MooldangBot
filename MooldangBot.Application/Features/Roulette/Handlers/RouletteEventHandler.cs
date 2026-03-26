using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Features.Roulette.Handlers;

public class RouletteEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RouletteEventHandler> _logger;
    private readonly IOverlayNotificationService _overlayService;

    public RouletteEventHandler(
        IServiceProvider serviceProvider, 
        ILogger<RouletteEventHandler> logger,
        IOverlayNotificationService overlayService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _overlayService = overlayService;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        var profile = notification.Profile;
        var chzzkUid = profile.ChzzkUid;
        var senderId = notification.SenderId;
        var msg = notification.Message.Trim();
        var donationAmount = notification.DonationAmount;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var rouletteService = scope.ServiceProvider.GetRequiredService<IRouletteService>();

        if (donationAmount > 0)
        {
            var cheeseRoulettes = await db.Roulettes
                .Where(r => r.ChzzkUid == chzzkUid && r.Type == RouletteType.Cheese && r.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var roulette in cheeseRoulettes)
            {
                string firstWord = string.IsNullOrEmpty(msg) ? "" : msg.Split(' ')[0];
                if (donationAmount >= roulette.CostPerSpin && firstWord == roulette.Command)
                {
                    int totalSpins = (int)(donationAmount / roulette.CostPerSpin);
                    _logger.LogInformation($"🎰 [룰렛 다회차 실행] {notification.Username}님 {donationAmount}치즈 후원 -> {roulette.Name} (총 {totalSpins}회)");
                    await rouletteService.SpinRouletteMultiAsync(chzzkUid, roulette.Id, totalSpins, notification.Username);
                }
            }
        }

        var pointRoulettes = await db.Roulettes
            .Where(r => r.ChzzkUid == chzzkUid && r.Type == RouletteType.ChatPoint && r.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var roulette in pointRoulettes)
        {
            if (string.IsNullOrEmpty(msg)) continue;
            string firstWord = msg.Split(' ')[0];
            if (firstWord == roulette.Command)
            {
                var viewer = await db.ViewerProfiles
                    .FirstOrDefaultAsync(v => v.StreamerChzzkUid == chzzkUid && v.ViewerUid == senderId, cancellationToken);

                if (viewer == null || viewer.Points < roulette.CostPerSpin)
                {
                    _logger.LogWarning($"⚠️ [룰렛 실행 실패] {notification.Username}님 포인트 부족");
                    continue;
                }

                viewer.Points -= roulette.CostPerSpin;
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"🎰 [룰렛 실행] {notification.Username}님 포인트 차감 -> {roulette.Name}");
                await rouletteService.SpinRouletteAsync(chzzkUid, roulette.Id, notification.Username);
            }
        }
    }
}
