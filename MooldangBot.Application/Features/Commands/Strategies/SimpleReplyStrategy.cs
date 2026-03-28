using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Features.Commands.Strategies;

/// <summary>
/// [하모니의 메아리]: 단순 채팅 답변(Reply) 또는 상단 공지(Notice)를 처리하는 기본 전략입니다.
/// </summary>
public class SimpleReplyStrategy(
    IChzzkBotService botService,
    IOverlayNotificationService overlayService,
    IDynamicQueryEngine dynamicEngine, // [v1.8] 엔진 주입
    ILogger<SimpleReplyStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Reply";

    public async Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        // 1. [인수 추출]: {내용} 치환용
        string msg = notification.Message.Trim();
        string args = msg.Length > command.Keyword.Length ? msg.Substring(command.Keyword.Length).Trim() : "";

        // 2. [1차 치환]: 시스템 변수 ({내용})
        string processedReply = command.ResponseText.Replace("{내용}", args, StringComparison.OrdinalIgnoreCase);

        // 3. [2차 치환]: 안전한 동적 쿼리 엔진 활용 ({포인트}, {닉네임} 등)
        processedReply = await dynamicEngine.ProcessMessageAsync(
            processedReply, 
            notification.Profile.ChzzkUid, 
            notification.SenderId
        );

        logger.LogInformation($"💬 [답변 실행] {notification.Username} -> {command.Keyword}");
        await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
        
    }
}
