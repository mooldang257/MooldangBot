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
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // --- 오마카세 활성화 체크 (DB에서 최신 정보 직접 조회) ---
        var currentProfile = await db.StreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == notification.Profile.ChzzkUid, cancellationToken);

        if (currentProfile == null || !currentProfile.IsOmakaseEnabled)
        {
            _logger.LogDebug($"[오마카세 무시] {notification.Profile.ChzzkUid}의 오마카세 기능이 비활성화 상태이거나 프로필을 찾을 수 없습니다.");
            return;
        }
        // --------------------------------------------------

        string msg = notification.Message.Trim();
        
        // 1. 스트리머의 모든 오마카세 동적 메뉴 가져오기
        var omakaseItems = await db.StreamerOmakases
            .Where(o => o.ChzzkUid == notification.Profile.ChzzkUid)
            .ToListAsync(cancellationToken);

        if (omakaseItems.Count == 0) return;

        // 2. 메시지/명령어 매칭 확인 (유연한 시작 단어 매칭)
        var matchedItem = omakaseItems.FirstOrDefault(o => 
            msg.StartsWith(o.Command, StringComparison.OrdinalIgnoreCase));

        if (matchedItem != null)
        {
            _logger.LogInformation($"🍱 [오마카세 포착] {notification.Username}님 -> {matchedItem.Name} (명령어: {matchedItem.Command})");

            // 3. 증가 수량 계산 (후원 금액 비례)
            int increaseAmount = 1;
            if (matchedItem.Price > 0)
            {
                if (notification.DonationAmount < matchedItem.Price)
                {
                    _logger.LogWarning($"⚠️ [금액 부족] {matchedItem.Name} 요구: {matchedItem.Price}, 실제: {notification.DonationAmount}");
                    return; 
                }
                
                // 설정 금액의 배수만큼 카운트 합산 (예: 500원 설정, 1000원 후원 시 2개)
                increaseAmount = notification.DonationAmount / matchedItem.Price;
            }

            // 4. 카운트 증가 및 저장 (낙관적 동시성 제어 및 재시도 패턴 적용)
            int retryCount = 0;
            const int maxRetries = 3;
            bool saved = false;

            while (!saved && retryCount < maxRetries)
            {
                try
                {
                    matchedItem.Count += increaseAmount;
                    await db.SaveChangesAsync(cancellationToken);
                    saved = true;
                    _logger.LogInformation($"✅ [오마카세 카운트 증가 성공] {matchedItem.Name}: (+{increaseAmount})");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    _logger.LogWarning($"⚠️ [동시성 충돌 감지] {matchedItem.Name} 업데이트 재시도 중... ({retryCount}/{maxRetries})");

                    // 최신 데이터로 엔티티 내용 갱신 (Database Win 전략)
                    foreach (var entry in ex.Entries)
                    {
                        var dbValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                        if (dbValues != null)
                        {
                            entry.OriginalValues.SetValues(dbValues);
                        }
                    }
                    
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError($"❌ [오마카세 업데이트 실패] 최대 재시도 횟수를 초과했습니다: {matchedItem.Name}");
                        throw;
                    }
                }
            }

            // 5. 실시간 오버레이 갱신 신호 발송
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<OverlayHub>>();
            string? targetUid = notification.Profile.ChzzkUid;
            if (!string.IsNullOrEmpty(targetUid))
            {
                string groupName = targetUid.ToLower();
                await hubContext.Clients.Group(groupName).SendAsync("RefreshSongAndDashboard", cancellationToken: cancellationToken);
            }
        }
    }
}
