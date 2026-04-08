using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models.Chzzk;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Features.Commands.SystemMessage;

/// <summary>
/// [오시리스의 선포]: 방송 제목(!방제)을 실시간으로 변경하는 전략입니다. (v4.1.0)
/// </summary>
public class TitleStrategy(
    IChzzkApiClient chzzkApi,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    ILogger<TitleStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => CommandFeatureTypes.Title;

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        return await ExecuteInternalAsync(notification, command.Keyword, command.ResponseText, ct);
    }

    private async Task<CommandExecutionResult> ExecuteInternalAsync(ChatMessageReceivedEvent notification, string keyword, string responseTemplate, CancellationToken ct)
    {
        // 1. [정수 추출]: 명령어 키워드 이후의 텍스트를 새로운 방제로 인식
        string msg = notification.Message.Trim();
        string newTitle = msg.Length > keyword.Length ? msg.Substring(keyword.Length).Trim() : "";

        if (string.IsNullOrEmpty(newTitle))
        {
            string statusReply = await dynamicEngine.ProcessMessageAsync("현재 방제: {방제} 🖋️", notification.Profile.ChzzkUid, notification.SenderId);
            await botService.SendReplyChatAsync(notification.Profile, statusReply, notification.SenderId, ct);
            return CommandExecutionResult.Success();
        }

        // 1.1 [정화]: 40자 초과 시 자동 절삭
        if (newTitle.Length > 40)
        {
            newTitle = newTitle[..40];
        }

        // 2. [명령 하달]: 봇 엔진에게 방제 변경 명령 송출
        try
        {
            logger.LogInformation($"📡 [방제 변경 오더] {notification.Profile.ChzzkUid} -> {newTitle}");
            
            bool success = await botService.UpdateTitleAsync(notification.Profile, newTitle, notification.SenderId, ct);

            if (success)
            {
                string template = string.IsNullOrEmpty(responseTemplate) 
                    ? "✅ 방송 제목 변경 명령이 전달되었습니다! {내용} 🖋️" 
                    : responseTemplate;
                
                string processedReply = await dynamicEngine.ProcessMessageAsync(
                    template.Replace("{내용}", newTitle), 
                    notification.Profile.ChzzkUid, 
                    notification.SenderId
                );

                await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
                return CommandExecutionResult.Success();
            }
            
            return CommandExecutionResult.Failure("방제 변경 명령 발행에 실패했습니다.", shouldRefund: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"🔥 [TitleStrategy] 오류: {ex.Message}");
            return CommandExecutionResult.Failure("방제 처리 중 서버 오류가 발생했습니다.", shouldRefund: true);
        }
    }
}
