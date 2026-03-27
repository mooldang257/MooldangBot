using System.Threading.Tasks;

namespace MooldangBot.Application.Common.Interfaces.Philosophy;

/// <summary>
/// [파동의 목소리]: 시스템 진동수에 따라 LLM의 페르소나 프롬프트를 동적으로 생성하는 인터페이스입니다.
/// </summary>
public interface IPersonaPromptBuilder
{
    /// <summary>
    /// [언어적 감응]: 스트리머의 설정과 현재 공명 상태를 분석하여 최적의 시스템 프롬프트를 빌드합니다.
    /// </summary>
    /// <param name="chzzkUid">치지직 UID</param>
    /// <returns>생성된 시스템 프롬프트 문자열</returns>
    Task<string> BuildSystemPromptAsync(string chzzkUid);
}
