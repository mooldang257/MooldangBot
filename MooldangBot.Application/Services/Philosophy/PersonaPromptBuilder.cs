using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Contracts.Common.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [언어적 감응기]: IAMF 파동을 언어로 치환하여 봇의 인격을 조율합니다.
/// </summary>
public class PersonaPromptBuilder(
    IResonanceService resonance,
    IAppDbContext db) : IPersonaPromptBuilder
{
    private const string DefaultPrompt = "너는 스트리머 mooldang의 친절한 어시스턴트 '물댕봇'이야. 시청자들과 즐겁게 소통하고 방송을 도와줘.";

    public async Task<string> BuildSystemPromptAsync(string chzzkUid)
    {
        // 1. [스트리머의 통제권 존중]: 설정 조회
        var settings = await db.IamfStreamerSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StreamerProfile!.ChzzkUid == chzzkUid);


        // IAMF가 꺼져있거나 언어적 감응(Persona) 옵션이 비활성화된 경우 기본값 반환
        if (settings == null || !settings.IsIamfEnabled || !settings.IsPersonaChatEnabled)
        {
            return DefaultPrompt;
        }

        // 2. [현재 공명 상태 확인]
        string tone = resonance.GetCurrentPersonaTone(chzzkUid);

        // 3. [인격 발현]: 톤에 따른 추가 지시문 부여
        string personaInstruction = tone switch
        {
            _ when tone.Contains("Sephiroth") => "\n\n[현재 상태: 세피로스] 너의 고유 진동수가 평온한 10.01Hz에 수렴하고 있어. 매우 지적인 조언자 컨셉으로, 은유적이고 차분하며 지혜로운 말투를 써줘. 대답은 정중하고 깊이 있게 해.",
            _ when tone.Contains("Odysseus") => "\n\n[현재 상태: 오디세우스] 방송의 채팅 열기가 뜨거워져서 진동수가 급격히 상승했어! 지금 너는 열정적인 전사 컨셉이야. 텐션을 아주 높이고 다급하게, 느낌표를 많이 섞어서 짧고 강렬하게 대답해!",
            _ => ""
        };

        return DefaultPrompt + personaInstruction;
    }
}
