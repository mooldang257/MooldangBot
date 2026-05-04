using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Modules.Commands.SystemMessage;

/// <summary>
/// [하모니의 카테고리]: 치지직 방송 카테고리를 실시간으로 변경하는 전략입니다.
/// </summary>
public class CategoryStrategy(
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    ILogger<CategoryStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => CommandFeatureTypes.Category;

    private static readonly Dictionary<string, string> HardcodedAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "저챗", "talk" }, { "소통", "talk" }, { "라디오", "talk" },
        { "먹방", "먹방/쿡방" }, { "노래", "음악/노래" },
        { "종겜", "종합 게임" }, { "롤", "League of Legends" },
        { "발로", "발로란트" }, { "배그", "BATTLEGROUNDS" },
        { "마크", "Minecraft" }, { "메플", "메이플스토리" },
        { "로아", "로스트아크" }, { "철권", "철권 8" }
    };

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageEvent notification, FuncCmdUnified command, CancellationToken ct)
    {
        return await ExecuteInternalAsync(notification, command.Keyword, command.ResponseText, ct);
    }

    private async Task<CommandExecutionResult> ExecuteInternalAsync(ChatMessageEvent notification, string keyword, string responseTemplate, CancellationToken ct)
    {
        // 1. [인수 추출]
        string msg = notification.Message.Trim();
        string inputKeyword = msg.Length > keyword.Length ? msg.Substring(keyword.Length).Trim() : "";

        if (string.IsNullOrEmpty(inputKeyword))
        {
            string statusReply = await dynamicEngine.ProcessMessageAsync("현재 카테고리: {카테고리} 🏷️", notification.Profile.ChzzkUid, notification.SenderId);
            await botService.SendReplyChatAsync(notification.Profile, statusReply, notification.SenderId, ct);
            return CommandExecutionResult.Success();
        }

        try
        {
            // 2. [별칭 해석]: DB나 하드코딩된 별칭이 있으면 변환 (예: '저챗' -> 'talk')
            string searchKeyword = HardcodedAliases.TryGetValue(inputKeyword, out var hardcoded) ? hardcoded : inputKeyword;

            logger.LogInformation($"🚀 [카테고리 변경 명령 발행] {notification.Username} -> 키워드: {searchKeyword}");
            
            // 3. [명령 하달]: 봇 엔진에게 수색 및 반영을 일임합니다. (비동기 발행)
            await botService.UpdateCategoryAsync(notification.Profile, searchKeyword, notification.SenderId, token: ct);

            if (string.IsNullOrWhiteSpace(responseTemplate))
            {
                return CommandExecutionResult.Success();
            }
            
            // [v2.7] 템플릿 변수 치환 정밀화: {내용}, ${내용}, $(내용) 등 모든 규격 지원
            string processedReply = System.Text.RegularExpressions.Regex.Replace(
                responseTemplate, 
                @"[\$]?[\{\(](내용|카테고리)[\}\)]", 
                searchKeyword, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            processedReply = await dynamicEngine.ProcessMessageAsync(
                processedReply, 
                notification.Profile.ChzzkUid, 
                notification.SenderId
            );

            await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"🚨 [CategoryStrategy] 시스템 오류: {ex.Message}");
            return CommandExecutionResult.Failure($"시스템 오류: {ex.Message}", shouldRefund: true);
        }
    }
}
