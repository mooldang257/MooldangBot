using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;

namespace MooldangBot.Application.Features.Chat.Handlers;

/// <summary>
/// [통합의 완성]: IAMF의 모든 엔진을 결합하여 실전 채팅 응답을 수행하는 최종 핸들러입니다.
/// </summary>
public class ChatInteractionHandler(
    IChatIntentRouter intentRouter,
    ILlmService llmService,
    IChzzkChatService chatService,
    IPhoenixRecorder phoenix, // [v2.0.2] AI 발화 기록을 위해 추가
    IServiceProvider serviceProvider,
    ILogger<ChatInteractionHandler> logger) : INotificationHandler<ChatMessageReceivedEvent>
{
    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug($"[채팅 핸들러 수신] Channel: {notification.Profile.ChzzkUid}, User: {notification.Username}, Msg: {notification.Message}");
        // [v2.3] 통계 집계(Scribe)는 이제 봇 엔진(ChzzkAPI)에서 직접 수행하므로 여기서는 제거합니다.
        
        // 1. [무한 루프 및 명령어 방지]: 봇 자신의 말이거나 시스템 메시지, 혹은 명령어(!)에는 반응하지 않음
        if (notification.Username.Contains("MooldangBot") || 
            string.IsNullOrEmpty(notification.SenderId) ||
            notification.Message.TrimStart().StartsWith('!'))
        {
            return;
        }

        // 1. [거울의 봉인]: 일반 채팅에 대한 자동 AI 응답 기능을 비활성화합니다. (v2.1.4)
        // 사용자의 요청에 따라 모든 AI 발화는 '통합 명령어(!질문 등)'를 통해서만 수행됩니다.
        /* 
        [기존 자동 응답 로직 - 비활성화됨]
        bool isStreamer = notification.SenderId == notification.Profile.ChzzkUid;
        string? systemPrompt = await intentRouter.RouteAndProcessChatAsync(...);
        ...
        */

        return; // 현재 핸들러는 메시지 기록(Scribe) 이후 프로세스를 종료합니다.
    }
}
