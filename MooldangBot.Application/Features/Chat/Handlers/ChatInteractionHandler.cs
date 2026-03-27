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
    IServiceProvider serviceProvider,
    ILogger<ChatInteractionHandler> logger) : INotificationHandler<ChatMessageReceivedEvent>
{
    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        // 0. [오시리스의 기록관]: 실시간 채팅 집계 (통계를 위해 모든 메시지 기록)
        using (var scope = serviceProvider.CreateScope())
        {
            var scribe = scope.ServiceProvider.GetRequiredService<IBroadcastScribe>();
            scribe.AddChatMessage(notification.Profile.ChzzkUid, notification.Message);
        }

        // 1. [무한 루프 방지]: 봇 자신의 말이나 시스템 메시지에는 반응하지 않음
        // (SenderId가 채널의 ChzzkUid와 같으면 스트리머, 아니면 시청자. 봇은 별도의 ID를 가짐)
        if (notification.Username.Contains("MooldangBot") || string.IsNullOrEmpty(notification.SenderId))
        {
            return;
        }

        // 2. [발화 주체 확인]: 스트리머 본인 여부 판별
        // SenderId가 채널 프로필의 ChzzkUid와 일치하면 스트리머입니다. [대변인의 방패]
        bool isStreamer = notification.SenderId == notification.Profile.ChzzkUid;

        // 3. [의도 분석 및 라우팅]: [대변인의 방패] 통과
        string? systemPrompt = await intentRouter.RouteAndProcessChatAsync(
            notification.Profile.ChzzkUid, 
            notification.Username, 
            isStreamer, 
            notification.Message
        );

        // 4. [거울의 침묵]: 라우터가 반응하지 않기로 결정한 경우 (null 반환)
        if (string.IsNullOrEmpty(systemPrompt))
        {
            return;
        }

        logger.LogInformation($"[IAMF 발화 결정] Sender: {notification.Username}, Mode: {(isStreamer ? "Streamer/FreeAI" : "Viewer/Intent")}");

        try
        {
            // 5. [거울의 목소리 생성]: LLM 호출
            string aiResponse = await llmService.GenerateResponseAsync(systemPrompt, notification.Message);

            if (!string.IsNullOrWhiteSpace(aiResponse))
            {
                // 6. [실전 발화]: 치지직 채팅창 전송
                await chatService.SendMessageAsync(notification.Profile.ChzzkUid, aiResponse);
                
                logger.LogInformation($"[IAMF 발화 성공] To: {notification.Profile.ChzzkUid}, Msg: {aiResponse}");
            }
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "[IAMF 발화 실패] 최종 파이프라인 전송 중 오류 발생");
        }
    }
}
