using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Events;
using MooldangBot.ChzzkAPI.Contracts.Models.Events;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Features.SongBook.Handlers;

/// <summary>
/// [서기의 노래]: 신청곡 및 오마카세 메뉴를 처리하는 최종 핸들러입니다. (v3.7)
/// </summary>
public class OmakaseEventHandler(
    IServiceProvider serviceProvider,
    IPointTransactionService pointService,
    IChzzkBotService botService,
    ILogger<OmakaseEventHandler> logger) : INotificationHandler<ChzzkEventReceived>
{
    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        // 1. [다형성 파라미터화]: 채팅과 후원 이벤트를 통합 파라미터로 추출
        if (notification.Payload is not (ChzzkChatEvent or ChzzkDonationEvent))
            return;

        var profile = notification.Profile;
        if (!profile.IsActive || !profile.IsMasterEnabled) return;

        bool isDonation = notification.Payload is ChzzkDonationEvent;
        var chat = notification.Payload as ChzzkChatEvent;
        var donation = notification.Payload as ChzzkDonationEvent;

        string message = (isDonation ? donation!.DonationMessage : chat!.Content).Trim();
        int payAmount = isDonation ? donation!.PayAmount : 0;
        string senderNickname = isDonation ? donation!.Nickname : chat!.Nickname;
        string senderId = isDonation ? donation!.SenderId : chat!.SenderId;

        if (string.IsNullOrEmpty(message)) return;

        // 2. [명령어 대조]: 긴 단어 우선순위 키워드 매칭
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var allActiveCommands = await db.UnifiedCommands
            .AsNoTracking()
            .Where(c => c.StreamerProfileId == profile.Id && c.IsActive)
            .ToListAsync(ct);

        var triggerCmd = allActiveCommands
            .OrderByDescending(c => c.Keyword.Length)
            .FirstOrDefault(c => message.StartsWith(c.Keyword, StringComparison.OrdinalIgnoreCase));

        if (triggerCmd == null) return;

        var featureType = triggerCmd.FeatureType;
        bool isSongRequestFeature = featureType == CommandFeatureType.SongRequest;
        bool isOmakaseFeature = featureType == CommandFeatureType.Omakase;

        if (!isSongRequestFeature && !isOmakaseFeature) return;

        // 3. [결제 보안]: CostType에 따른 실질적 검증 및 차감
        int requiredPrice = triggerCmd.Cost;

        if (triggerCmd.CostType == CommandCostType.Cheese)
        {
            // 치즈 후원 명령어는 반드시 후원 이벤트를 통해서만 실행 가능
            if (!isDonation || payAmount < requiredPrice) return;
        }
        else if (triggerCmd.CostType == CommandCostType.Point)
        {
            // 포인트 명령어는 시청자 잔액 차감 시도
            var (success, _) = await pointService.AddPointsAsync(
                profile.ChzzkUid!, senderId, senderNickname, -requiredPrice, ct);

            if (!success)
            {
                await botService.SendReplyChatAsync(profile, 
                    $"❌ @{senderNickname}님, 포인트가 부족합니다. (필요: {requiredPrice}P)", senderId, ct);
                return;
            }
        }

        // 4. [신청곡 세션 확인]: 일반 노래 신청의 경우 영업 중인지 확인
        if (isSongRequestFeature)
        {
            var activeSession = await db.SonglistSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StreamerProfileId == profile.Id && s.IsActive, ct);
                
            if (activeSession == null) return;
        }

        // 5. [데이터 가공]: 곡 제목 추출
        string songTitle = string.Empty;

        if (isSongRequestFeature)
        {
            songTitle = message.Substring(triggerCmd.Keyword.Length).Trim();
            if (string.IsNullOrEmpty(songTitle))
            {
                await botService.SendReplyChatAsync(profile, 
                    $"@{senderNickname}님, 신청하실 곡 제목을 입력해주세요! (예: {triggerCmd.Keyword} 곡제목)", senderId, ct);
                return;
            }
        }
        else if (isOmakaseFeature)
        {
            var selectedMenu = await db.StreamerOmakases
                .FirstOrDefaultAsync(o => o.StreamerProfileId == profile.Id && o.Id == triggerCmd.TargetId && o.IsActive, ct);

            if (selectedMenu == null) return;

            // ResponseText는 설명용으로만 사용 (곡 제목 구성 요소)
            songTitle = $"{selectedMenu.Icon} {triggerCmd.ResponseText}";
            selectedMenu.Count++;
        }

        // 6. [최종 집행]: SongQueue 저장 (성공 시 침묵 정책 유지)
        try
        {
            var queueCount = await db.SongQueues
                .Where(q => q.StreamerProfileId == profile.Id)
                .CountAsync(ct);

            var newRequest = new MooldangBot.Domain.Entities.SongQueue
            {
                StreamerProfileId = profile.Id,
                Title = songTitle,
                Status = SongStatus.Pending,
                CreatedAt = KstClock.Now,
                SortOrder = queueCount + 1
            };

            db.SongQueues.Add(newRequest);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("✅ [Song Request Success] {Nickname} -> {Title} (CostType: {CostType})", 
                senderNickname, songTitle, triggerCmd.CostType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Song Request Failed] DB 저장 중 오류 발생");
        }
    }
}
