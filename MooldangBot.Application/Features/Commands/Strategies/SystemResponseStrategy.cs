using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Features.Commands.Strategies;

/// <summary>
/// [파로스의 가변]: 시스템(System) 응답 내용을 실시간으로 수정하거나 조회하는 전략입니다.
/// </summary>
public class SystemResponseStrategy(
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    IServiceProvider serviceProvider,
    ILogger<SystemResponseStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Notice";

    public async Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        // 1. [인수 추출]
        string msg = notification.Message.Trim();
        string args = msg.Length > command.Keyword.Length ? msg.Substring(command.Keyword.Length).Trim() : "";

        // 1.1 [정화]: 공지(Notice) 타입일 경우 100자 자동 절삭
        if (command.FeatureType == "Notice" && args.Length > 100)
        {
            args = args[..100];
        }

        if (string.IsNullOrEmpty(args))
        {
            logger.LogInformation($"🔍 [시스템 응답 실행] {notification.Username} -> {command.Keyword}");
            
            string processedReply = await dynamicEngine.ProcessMessageAsync(
                command.ResponseText, 
                notification.Profile.ChzzkUid, 
                notification.SenderId
            );

            if (command.FeatureType == "Notice")
            {
                using var scope = serviceProvider.CreateScope();
                var overlayService = scope.ServiceProvider.GetRequiredService<IOverlayNotificationService>();
                await overlayService.NotifyRefreshAsync(notification.Profile.ChzzkUid, ct);

                bool success = await botService.SendReplyNoticeAsync(notification.Profile, processedReply, notification.SenderId, ct);

                if (success)
                {
                    logger.LogInformation($"📢 [공지 등록 성공] {notification.Username} -> {processedReply}");
                }
                else
                {
                    await botService.SendReplyChatAsync(notification.Profile, "⚠️ [공지 실패] 치지직 상단 공지 등록에 실패했습니다.", notification.SenderId, ct);
                }
            }
            else
            {
                await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
            }
            return;
        }

        try
        {
            // 2. [상태 변경 (Update)]: 인수가 있으면 ResponseText 업데이트
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            var dbCommand = await db.UnifiedCommands.FindAsync(command.Id);
            if (dbCommand != null)
            {
                dbCommand.ResponseText = args;
                await db.SaveChangesAsync(ct);
                
                // 🏷️ [v1.9.8] 공지(Notice) 타입인 경우 오버레이 새로고침 및 즉시 공지 출력 (UX 개선)
                if (command.FeatureType == "Notice")
                {
                    var overlayService = scope.ServiceProvider.GetRequiredService<IOverlayNotificationService>();
                    await overlayService.NotifyRefreshAsync(notification.Profile.ChzzkUid, ct);

                    // 1.9.9 개선: 채팅창 출력 대신 치지직 플랫폼 '상단 공지'로 실제 등록
                    string processedReply = await dynamicEngine.ProcessMessageAsync(args, notification.Profile.ChzzkUid, notification.SenderId);
                    bool success = await botService.SendReplyNoticeAsync(notification.Profile, processedReply, notification.SenderId, ct);

                    if (success)
                    {
                        logger.LogInformation($"📢 [공지 등록 성공] {notification.Username} -> {processedReply}");
                    }
                    else
                    {
                        // 실패 시에는 사용자에게 알림 (권한 부족 등)
                        await botService.SendReplyChatAsync(notification.Profile, "⚠️ [공지 실패] 치지직 상단 공지 등록에 실패했습니다. (권한 부족 또는 API 제한) 🚫", notification.SenderId, ct);
                    }
                }
                else
                {
                    logger.LogInformation($"📝 [시스템 응답 수정] {notification.Username} -> {command.Keyword}: {args}");
                    await botService.SendReplyChatAsync(notification.Profile, $"✅ '{command.Keyword}' 응답이 성공적으로 변경되었습니다. (Osiris's Record) 📜", notification.SenderId, ct);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"🔥 [SystemResponseStrategy] 오류: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "⚠️ 시스템 처리 중 서버 오류가 발생했습니다. 🌪️", notification.SenderId, ct);
        }
    }
}
