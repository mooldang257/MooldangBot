using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Application.Common.Interfaces;
using System;
using MooldangBot.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Commands.Feature;

/// <summary>
/// [지능의 동적 발현]: 스트리머가 커스텀한 명령어(!질문 등)를 통해 AI 응답을 수행하는 전략입니다. (v2.1.0)
/// </summary>
public class AiResponseStrategy(
    ILlmService llmService,
    IChzzkBotService botService,
    IPersonaPromptBuilder promptBuilder,
    IPhoenixRecorder phoenix,
    ILogger<AiResponseStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "AI";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        // ... (생략된 로직)
        string fullMessage = notification.Message.Trim();
        string firstWord = fullMessage.Split(' ')[0];
        string aiPrompt = fullMessage.Length > firstWord.Length 
                            ? fullMessage.Substring(firstWord.Length).Trim() 
                            : command.ResponseText;

        try 
        {
            var basePrompt = await promptBuilder.BuildSystemPromptAsync(notification.Profile.ChzzkUid);
            string finalSystemPrompt = $"{basePrompt}\n\n[동적 명령어 모드]: 너는 현재 시청자의 특정 명령어({command.Keyword})에 응답 중이다. 사용자의 질문에 지혜롭게 답해라.";

            string aiResponse = await llmService.GenerateResponseAsync(finalSystemPrompt, aiPrompt);

            if (!string.IsNullOrWhiteSpace(aiResponse))
            {
                await botService.SendReplyChatAsync(notification.Profile, aiResponse, notification.SenderId, ct);
                await phoenix.RecordScenarioAsync(
                    scenarioId: $"AI_CMD-{KstClock.Now:yyyyMMddHHmmss}",
                    content: $"[CMD:{command.Keyword} from {notification.Username}] {aiResponse}",
                    level: 1
                );
            }
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"❌ [AiResponseStrategy] AI 호출 파동 붕괴: {command.Keyword}");
            await botService.SendReplyChatAsync(notification.Profile, "❌ AI 신경망에 일시적인 과부하가 발생했습니다. (Resonance Collapse) 💦", notification.SenderId, ct);
            return CommandExecutionResult.Failure("AI 호출 중 오류가 발생했습니다.", shouldRefund: true);
        }
    }
}
