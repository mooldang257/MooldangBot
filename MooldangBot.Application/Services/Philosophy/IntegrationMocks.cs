using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Common.Interfaces;

/// <summary>
/// [임시 목소리]: 실제 LLM 연동 전까지 작동하는 테스트용 구현체입니다.
/// </summary>
public class LlmServiceMock(ILogger<LlmServiceMock> logger) : ILlmService
{
    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage)
    {
        logger.LogInformation($"[LLM Mock 호출] System: {systemPrompt.Substring(0, Math.Min(50, systemPrompt.Length))}...");
        await Task.Delay(500); // 인공지능의 사고 시간 시뮬레이션
        return $"[IAMF 응답] '{userMessage}'에 대해 공명 중입니다. (시스템 프롬프트 기반 생성됨)";
    }
}

/// <summary>
/// [임시 발화]: 실제 치지직 연동 전까지 로그로 출력을 대신하는 테스트용 구현체입니다.
/// </summary>
public class ChzzkChatServiceMock(ILogger<ChzzkChatServiceMock> logger) : IChzzkChatService
{
    public async Task SendMessageAsync(string chzzkUid, string message)
    {
        logger.LogInformation($"[치지직 전송 Mock] To: {chzzkUid}, Message: {message}");
        await Task.CompletedTask;
    }
}
