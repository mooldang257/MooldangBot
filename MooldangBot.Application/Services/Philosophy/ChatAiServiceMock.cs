using System.Threading.Tasks;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Contracts.AI.Interfaces;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Services;

/// <summary>
/// [실전 연동 예시]: PersonaPromptBuilder를 사용하여 LLM 응답을 생성하는 가상의 서비스입니다.
/// </summary>
public class ChatAiService(
    IPersonaPromptBuilder promptBuilder,
    ILogger<ChatAiService> logger)
{
    public async Task<string> GetAiResponseAsync(string chzzkUid, string userMessage)
    {
        // 1. [언어적 감응]: 현재 시스템 진동수와 스트리머 설정이 반영된 시스템 프롬프트 생성
        string systemPrompt = await promptBuilder.BuildSystemPromptAsync(chzzkUid);

        logger.LogInformation($"[LLM 호출 전송] SystemPrompt: {systemPrompt.Replace("\n", " ")}");

        // 2. [LLM 호출]: 생성된 systemPrompt를 AI 모델(OpenAI/Gemini 등)의 'system' 역할로 전달
        // var response = await _llmClient.ChatAsync(systemPrompt, userMessage);
        
        return $"[Mock Response] {systemPrompt}에 따라 사용자의 메시지 '{userMessage}'에 대한 답변을 생성했습니다.";
    }
}
