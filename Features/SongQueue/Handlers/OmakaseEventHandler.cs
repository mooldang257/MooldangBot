using MediatR;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;
using Microsoft.EntityFrameworkCore;

namespace MooldangAPI.Features.SongQueue.Handlers;

public class OmakaseEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OmakaseEventHandler> _logger;

    public OmakaseEventHandler(IServiceProvider serviceProvider, ILogger<OmakaseEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        var profile = notification.Profile;
        string msg = notification.Message.Trim();
        
        // 1. 스트리머의 모든 오마카세 동적 메뉴 가져오기
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var omakaseItems = await db.StreamerOmakases
            .Where(o => o.ChzzkUid == profile.ChzzkUid)
            .ToListAsync(cancellationToken);

        if (omakaseItems.Count == 0) return;

        // 2. 메시지/명령어 매칭 확인 (전체 일치 또는 시작 단어 일치)
        string firstWord = msg.Split(' ')[0];
        var matchedItem = omakaseItems.FirstOrDefault(o => o.Command == msg || o.Command == firstWord);

        if (matchedItem != null)
        {
            _logger.LogInformation($"🍱 [오마카세 포착] {notification.Username}님 -> {matchedItem.Name} (명령어: {matchedItem.Command})");

            // 3. 후원 금액 조건 확인 (설정된 금액이 있는 경우)
            if (matchedItem.CheesePrice > 0)
            {
                if (notification.DonationAmount < matchedItem.CheesePrice)
                {
                    _logger.LogWarning($"⚠️ [금액 부족] {matchedItem.Name} 요구: {matchedItem.CheesePrice}, 실제: {notification.DonationAmount}");
                    return; 
                }
            }

            // 4. 카운트 증가 및 저장
            matchedItem.Count++;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"✅ [오마카세 카운트 증가] {matchedItem.Name}: {matchedItem.Count-1} -> {matchedItem.Count}");

            // 5. 실시간 오버레이 갱신 신호 발송
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<OverlayHub>>();
            await hubContext.Clients.Group(profile.ChzzkUid).SendAsync("RefreshSonglist", cancellationToken: cancellationToken);
        }
    }
}
