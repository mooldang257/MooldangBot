using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Features.Chat.Events;
using MooldangAPI.Models;
using MooldangAPI.Services;

namespace MooldangAPI.Features.Roulette.Handlers
{
    public class RouletteEventHandler : INotificationHandler<ChatMessageReceivedEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RouletteEventHandler> _logger;

        public RouletteEventHandler(IServiceProvider serviceProvider, ILogger<RouletteEventHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
        {
            var profile = notification.Profile;
            var chzzkUid = profile.ChzzkUid;
            var senderId = notification.SenderId;
            var msg = notification.Message.Trim();
            var donationAmount = notification.DonationAmount;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rouletteService = scope.ServiceProvider.GetRequiredService<RouletteService>();

            // 1. 치즈 후원 처리 (Type == Cheese)
            if (donationAmount > 0)
            {
                var cheeseRoulettes = await db.Roulettes
                    .Where(r => r.ChzzkUid == chzzkUid && r.Type == RouletteType.Cheese && r.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var roulette in cheeseRoulettes)
                {
                    // 명령어 첫 단어가 일치하는지 확인
                    string firstWord = string.IsNullOrEmpty(msg) ? "" : msg.Split(' ')[0];

                    if (donationAmount >= roulette.CostPerSpin && firstWord == roulette.Command)
                    {
                        // 10연차 판별 (금액이 10배 이상이면 10연차 실행)
                        if (donationAmount >= roulette.CostPerSpin * 10)
                        {
                            _logger.LogInformation($"🎰 [룰렛 10연차 실행] {notification.Username}님 {donationAmount}치즈 후원 -> {roulette.Name}");
                            await rouletteService.SpinRoulette10xAsync(chzzkUid, roulette.Id, notification.Username);
                        }
                        else
                        {
                            _logger.LogInformation($"🎰 [룰렛 1회 실행] {notification.Username}님 {donationAmount}치즈 후원 -> {roulette.Name}");
                            await rouletteService.SpinRouletteAsync(chzzkUid, roulette.Id, notification.Username);
                        }
                    }
                }
            }

            // 2. 채팅 명령어 처리 (Type == ChatPoint)
            var pointRoulettes = await db.Roulettes
                .Where(r => r.ChzzkUid == chzzkUid && r.Type == RouletteType.ChatPoint && r.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var roulette in pointRoulettes)
            {
                string firstWord = msg.Split(' ')[0];
                if (firstWord == roulette.Command)
                {
                    // 시청자 포인트 확인
                    var viewer = await db.ViewerProfiles
                        .FirstOrDefaultAsync(v => v.StreamerChzzkUid == chzzkUid && v.ViewerUid == senderId, cancellationToken);

                    if (viewer == null || viewer.Points < roulette.CostPerSpin)
                    {
                        _logger.LogWarning($"⚠️ [룰렛 실행 실패] {notification.Username}님 포인트 부족 ({viewer?.Points ?? 0} < {roulette.CostPerSpin})");
                        continue;
                    }

                    // 포인트 차감 및 룰렛 실행
                    viewer.Points -= roulette.CostPerSpin;
                    await db.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation($"🎰 [룰렛 실행] {notification.Username}님 포인트 차감 -> {roulette.Name}");
                    await rouletteService.SpinRouletteAsync(chzzkUid, roulette.Id, notification.Username);
                }
            }
        }
    }
}
