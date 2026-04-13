using System.Threading.Tasks;

namespace MooldangBot.Application.Common.Interfaces;

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

/// <summary>
/// [실전 발화]: 생성된 메시지를 실제 치지직 채팅창으로 전송하는 인터페이스입니다.
/// </summary>
public interface IChzzkChatService
{
    /// <summary>
    /// [거울의 울림]: 특정 스트리머의 채팅창에 메시지를 전송합니다. (v1.9 동적 엔진용 viewerUid 추가)
    /// </summary>
    Task SendMessageAsync(string chzzkUid, string message, string viewerUid, System.Threading.CancellationToken ct = default);
}
