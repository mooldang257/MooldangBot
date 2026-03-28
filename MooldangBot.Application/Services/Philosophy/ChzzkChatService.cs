using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [실전 발화 구현체]: 생성된 AI 답변을 실제 치지직 채팅창으로 전송합니다. (v2.1.2)
/// </summary>
public class ChzzkChatService(
    IChzzkBotService botService,
    IServiceScopeFactory scopeFactory) : IChzzkChatService
{
    public async Task SendMessageAsync(string chzzkUid, string message, string viewerUid)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // 최신 스트리머 프로필 로드 (영속성 보장 및 격리된 상태 유지)
        var profile = await db.StreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

        if (profile != null)
        {
            // [v2.1.2] IChzzkBotService를 통해 실제 채팅 전송 (접두어 및 글자수 제한 자동 처리)
            await botService.SendReplyChatAsync(profile, message, viewerUid, System.Threading.CancellationToken.None);
        }
    }
}
