namespace MooldangBot.Domain.Contracts.AI.Interfaces;

/// <summary>
/// [목소리의 생성]: LLM(OpenAI, Gemini 등)을 통해 실제 응답 텍스트를 생성하는 인터페이스입니다.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// [언어적 발현]: 시스템 프롬프트와 사용자 메시지를 기반으로 AI 답변을 생성합니다.
    /// </summary>
    Task<string> GenerateResponseAsync(string systemPrompt, string userMessage);
}
