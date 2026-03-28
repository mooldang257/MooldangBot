using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Commands.Strategies;

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

    public async Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        // 1. [지혜의 추출]: 명령어 키워드 뒤의 내용을 질문으로 감지
        string fullMessage = notification.Message.Trim();
        string firstWord = fullMessage.Split(' ')[0];
        
        // 질문이 있으면 사용자 입력을, 없으면 DB에 설정된 기본 문구를 프롬프트로 사용
        string aiPrompt = fullMessage.Length > firstWord.Length 
                            ? fullMessage.Substring(firstWord.Length).Trim() 
                            : command.ResponseText;

        logger.LogInformation($"🧠 [AI 동적 명령 감지] {notification.Username} -> {command.Keyword}: {aiPrompt}");

        try 
        {
            // 2. [페르소나의 중첩]: 스트리머 고유의 AI 설정 주입
            var basePrompt = await promptBuilder.BuildSystemPromptAsync(notification.Profile.ChzzkUid);
            string finalSystemPrompt = $"{basePrompt}\n\n[동적 명령어 모드]: 너는 현재 시청자의 특정 명령어({command.Keyword})에 응답 중이다. 사용자의 질문에 지혜롭게 답해라.";

            // 3. [거울의 목소리 생성]: LLM 호출기 (v2.0.1 회복성 패턴 적용됨)
            string aiResponse = await llmService.GenerateResponseAsync(finalSystemPrompt, aiPrompt);

            if (!string.IsNullOrWhiteSpace(aiResponse))
            {
                // 치지직 글자수 제한(500자) 및 접두어 처리는 하위 서비스(ChzzkBotService)에서 자동 수행됩니다.
                
                // 4. [실전 발화]: 치지직 채팅창 전송
                await botService.SendReplyChatAsync(notification.Profile, aiResponse, notification.SenderId, ct);
                
                // 5. [피닉스의 기록]: AI 발화 내역 영속화 (v2.0.2 패턴)
                await phoenix.RecordScenarioAsync(
                    scenarioId: $"AI_CMD-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    content: $"[CMD:{command.Keyword} from {notification.Username}] {aiResponse}",
                    level: 1
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"❌ [AiResponseStrategy] AI 호출 파동 붕괴: {command.Keyword}");
            await botService.SendReplyChatAsync(notification.Profile, "❌ AI 신경망에 일시적인 과부하가 발생했습니다. (Resonance Collapse) 💦", notification.SenderId, ct);
        }
    }
}
