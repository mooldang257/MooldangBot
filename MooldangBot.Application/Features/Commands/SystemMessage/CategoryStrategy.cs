using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.SystemMessage;

/// <summary>
/// [하모니의 카테고리]: 치지직 방송 카테고리를 실시간으로 변경하는 전략입니다.
/// </summary>
public class CategoryStrategy(
    IChzzkApiClient chzzkApi,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    IServiceProvider serviceProvider,
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

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        return await ExecuteInternalAsync(notification, command.Keyword, command.ResponseText, ct);
    }

    private async Task<CommandExecutionResult> ExecuteInternalAsync(ChatMessageReceivedEvent notification, string keyword, string responseTemplate, CancellationToken ct)
    {
        // [인수 추출]: 명령어 키워드 이후 텍스트를 검색 카테고리로 인식
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
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            var aliasObj = await db.ChzzkCategoryAliases
                .Include(a => a.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Alias == inputKeyword, ct);

            string searchKeyword = aliasObj?.Category?.CategoryValue ?? 
                                   (HardcodedAliases.TryGetValue(inputKeyword, out var hardcoded) ? hardcoded : inputKeyword);

            logger.LogInformation($"🔍 [카테고리 변경 요청] {notification.Username} -> 원본: {inputKeyword}, 검색어: {searchKeyword}");
            
            var searchResult = await chzzkApi.SearchCategoryAsync(searchKeyword);
            if (searchResult != null && searchResult.Code == 200 && searchResult.Content?.Data?.Count > 0)
            {
                var target = searchResult.Content.Data[0];
                logger.LogInformation($"✅ [카테고리 검색 결과] {target.CategoryValue} (Type: {target.CategoryType}, ID: {target.CategoryId})");
                
                // 💡 [중요] 치지직 Open API는 PATCH 시 평면 구조(Flat)를 기대합니다.
                // 전역 핸들러에서 토큰을 이미 최신화했으므로 프로필의 토큰을 신뢰할 수 있습니다.
                var updateData = new { categoryType = target.CategoryType, categoryId = target.CategoryId };
                bool success = await chzzkApi.UpdateLiveSettingAsync(notification.Profile.ChzzkAccessToken ?? "", updateData);

                if (success)
                {
                    logger.LogInformation($"🚀 [카테고리 변경 완료] {notification.Profile.ChzzkUid}: [{target.CategoryValue}]");
                    
                    string template = string.IsNullOrEmpty(responseTemplate) 
                        ? "✅ 카테고리가 [{내용}](으)로 성공적으로 변경되었습니다! 🎈" 
                        : responseTemplate;
                    
                    string processedReply = await dynamicEngine.ProcessMessageAsync(
                        template.Replace("{내용}", target.CategoryValue).Replace("{카테고리}", target.CategoryValue), 
                        notification.Profile.ChzzkUid, 
                        notification.SenderId
                    );

                    await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
                    return CommandExecutionResult.Success();
                }
                else
                {
                    logger.LogWarning($"⚠️ [카테고리 변경 실패] {notification.Profile.ChzzkUid} (권한 없는 토큰 또는 만료)");
                    await botService.SendReplyChatAsync(notification.Profile, "❌ 카테고리 변경 권한이 없거나 토큰이 만료되었습니다. 🔓", notification.SenderId, ct);
                    return CommandExecutionResult.Failure("API 업데이트 실패", shouldRefund: true);
                }
            }
            else
            {
                logger.LogWarning($"🕵️‍♂️ [카테고리 검색 실패] '{searchKeyword}' (공식 카테고리 없음)");
                await botService.SendReplyChatAsync(notification.Profile, $"⚠️ '{searchKeyword}'(으)로 검색되는 공식 카테고리가 없습니다. 정확한 명칭을 입력해 주세요. 🕵️‍♂️", notification.SenderId, ct);
                return CommandExecutionResult.Failure("카테고리를 찾을 수 없습니다.", shouldRefund: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"🚨 [CategoryStrategy] 시스템 오류: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "🚨 치지직 서버와 통신 중 예상치 못한 오류가 발생했습니다. 잠시 후 다시 시도해 주세요. 💫", notification.SenderId, ct);
            return CommandExecutionResult.Failure($"시스템 오류: {ex.Message}", shouldRefund: true);
        }
    }
}
