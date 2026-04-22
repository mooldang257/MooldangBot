using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Modules.Roulette.Notifications;
using MooldangBot.Modules.Roulette.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Modules.Roulette.Handlers;

/// <summary>
/// [오시리스의 마침표 전달자]: 오버레이로부터 연출 완료 신호를 받으면, 실제 채팅 결과 메시지를 발송합니다.
/// 지휘관님의 지침에 따라 애니메이션과 채팅 타이밍을 완벽하게 동기화합니다.
/// </summary>
public class RouletteCompletionNotificationHandler(
    IChzzkBotService botService,
    IRouletteDbContext db,
    ILogger<RouletteCompletionNotificationHandler> logger) : INotificationHandler<RouletteCompletionResultNotification>
{
    public async Task Handle(RouletteCompletionResultNotification notification, CancellationToken ct)
    {
        try
        {
            // 1. 스트리머 프로필 조회 (채팅 발신을 위한 권한 획득)
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ChzzkUid == notification.ChzzkUid, ct);

            if (streamer == null)
            {
                logger.LogWarning("⚠️ [완료 알림 무시] 스트리머 프로필을 찾을 수 없습니다. (ChzzkUid: {Uid})", notification.ChzzkUid);
                return;
            }

            // 2. 사격 데이터 준비 (요약 메시지)
            string message = $"🎰 [추첨 결과]: {notification.Summary}! 축하합니다! 🎉";

            // 3. 정밀 사격 개시 (채팅 전송)
            await botService.SendReplyChatAsync(streamer, message, notification.ViewerUid, ct);

            logger.LogInformation("🚀 [사격 완료] 룰렛 애니메이션 종료에 맞춰 채팅 메시지가 전송되었습니다. (Target: {Viewer})", notification.ViewerNickname);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [완료 알림 처리 실패] 채팅 메시지 전송 중 오류가 발생했습니다.");
        }
    }
}
