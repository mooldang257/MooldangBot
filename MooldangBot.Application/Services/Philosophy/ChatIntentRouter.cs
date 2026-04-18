using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using System.Linq;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [대변인의 방패]: 스트리머의 권위를 보호하고 의도된 지식만을 전달하는 라우터 구현체입니다.
/// </summary>
public class ChatIntentRouter(
    IAppDbContext db,
    IPersonaPromptBuilder promptBuilder) : IChatIntentRouter
{
    public async Task<string?> RouteAndProcessChatAsync(string chzzkUid, string senderUid, bool isStreamer, string message)
    {
        // 1. [주인의 목소리]: 스트리머 본인의 발화 처리
        if (isStreamer)
        {
            // [v2.1.3] 명시적 호출 감지: '물멍' 또는 '물댕' 키워드가 포함된 경우에만 반응 (일상 대화 간섭 방지)
            bool isCallingBot = message.Contains("물멍") || message.Contains("물댕");
            if (!isCallingBot) return null;

            // 스트리머의 채팅은 자유 AI 모드로 트리거됨
            var basePrompt = await promptBuilder.BuildSystemPromptAsync(chzzkUid);
            return $"{basePrompt}\n\n[자유 AI 모드 트리거]: 너는 현재 스트리머와 대화 중이다. 그의 질문에 대해 참모로서 지혜롭게 대답해라.";
        }

        // 2. [대변인의 방패]: 시청자 발화 처리
        // 키워드 매칭을 통한 지식 검색 [AsNoTracking]
        var knowledge = await db.StreamerKnowledges
            .AsNoTracking()
            .Where(k => k.StreamerProfile!.ChzzkUid == chzzkUid && k.IsActive)
            .ToListAsync();


        // 메시지 내에 포함된 키워드 검색
        var matchedKnowledge = knowledge.FirstOrDefault(k => message.Contains(k.Keyword, System.StringComparison.OrdinalIgnoreCase));

        // [거울의 침묵]: 일치하는 지식이 없으면 응답하지 않음
        if (matchedKnowledge == null)
        {
            return null;
        }

        // 3. [지식 기반 감응]: 매칭된 지식을 현재의 페르소나로 포장
        var personaPrompt = await promptBuilder.BuildSystemPromptAsync(chzzkUid);
        
        return $"{personaPrompt}\n\n" +
               $"[대변인 모드]: 시청자의 질문에 대해 스트리머가 미리 준비한 답변은 다음과 같다: [{matchedKnowledge.IntentAnswer}]. " +
               $"이 내용을 바탕으로 현재 너의 상태(톤)에 맞게 1문장으로 짧고 강렬하게 대답해라. 지식 외의 정보는 덧붙이지 마라.";
    }
}
