using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.Strategies;

/// <summary>
/// [오시리스의 유희]: 치지직 방송 카테고리를 실시간으로 변경하는 전략입니다. (v4.1.0)
/// </summary>
public class CategoryStrategy(
    IChzzkApiClient chzzkApi,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    IServiceProvider serviceProvider,
    ILogger<CategoryStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Category";

    private static readonly Dictionary<string, string> HardcodedAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "저챗", "talk" }, { "소통", "talk" }, { "노가리", "talk" },
        { "먹방", "먹방/쿡방" }, { "노래", "음악/노래" },
        { "종겜", "종합 게임" }, { "롤", "리그 오브 레전드" },
        { "발로", "발로란트" }, { "배그", "BATTLEGROUNDS" },
        { "마크", "Minecraft" }, { "메", "메이플스토리" },
        { "로아", "로스트아크" }, { "철권", "철권 8" }
    };

    public async Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        // 1. [정수 추출]: 명령어 키워드 이후의 텍스트를 검색 카테고리로 인식
        string msg = notification.Message.Trim();
        string inputKeyword = msg.Length > command.Keyword.Length ? msg.Substring(command.Keyword.Length).Trim() : "";

        if (string.IsNullOrEmpty(inputKeyword))
        {
            string statusReply = await dynamicEngine.ProcessMessageAsync("현재 카테고리: {카테고리} 🎮", notification.Profile.ChzzkUid, notification.SenderId);
            await botService.SendReplyChatAsync(notification.Profile, statusReply, notification.SenderId, ct);
            return;
        }

        // 2. [신성한 변모]: 카테고리 검색 및 변경 시도
        try
        {
            // 2.1 [별칭 조회]: DB 기반 사용자 정의 별칭 우선, 그 다음 하드코딩 별칭 처리
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            var aliasObj = await db.ChzzkCategoryAliases
                .Include(a => a.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Alias == inputKeyword, ct);

            string searchKeyword = aliasObj?.Category?.CategoryValue ?? 
                                   (HardcodedAliases.TryGetValue(inputKeyword, out var hardcoded) ? hardcoded : inputKeyword);

            logger.LogInformation($"🛠️ [카테고리 변경 요청] {notification.Username} -> 원본: {inputKeyword}, 검색어: {searchKeyword}");
            
            // 2.2 [신성한 탐색]: 치지직 공식 카테고리 정보 검색
            var searchResult = await chzzkApi.SearchCategoryAsync(searchKeyword);
            if (searchResult != null && searchResult.Code == 200 && searchResult.Content?.Data?.Count > 0)
            {
                var target = searchResult.Content.Data[0];
                
                // 2.3 [신성한 선언]: 실제 라이브 설정 업데이트
                var updateData = new { categoryType = target.CategoryType, categoryId = target.CategoryId };
                bool success = await chzzkApi.UpdateLiveSettingAsync(notification.Profile.ChzzkAccessToken ?? "", updateData);

                if (success)
                {
                    logger.LogInformation($"✨ [카테고리 변경 완료] {notification.Profile.ChzzkUid}: [{target.CategoryValue}]");
                    
                    // [v4.1.3] DB의 응답 템플릿 사용 (없으면 기본값)
                    string responseTemplate = string.IsNullOrEmpty(command.ResponseText) 
                        ? "✅ 카테고리가 [{내용}](으)로 성공적으로 변경되었습니다! 🎮" 
                        : command.ResponseText;
                    
                    // {내용}은 입력값으로 치환하고, 나머지는 엔진에게 맡김
                    string processedReply = await dynamicEngine.ProcessMessageAsync(
                        responseTemplate.Replace("{내용}", target.CategoryValue), 
                        notification.Profile.ChzzkUid, 
                        notification.SenderId
                    );

                    await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
                }
                else
                {
                    logger.LogWarning($"❌ [카테고리 변경 실패] {notification.Profile.ChzzkUid} (권한 또는 토큰 만료)");
                    await botService.SendReplyChatAsync(notification.Profile, "❌ 카테고리 변경에 실패했습니다. 스트리머의 권한 설정이나 토큰 상태를 확인해주세요. 🚫", notification.SenderId, ct);
                }
            }
            else
            {
                logger.LogWarning($"⚠️ [카테고리 검색 실패] '{searchKeyword}' (결과 없음)");
                await botService.SendReplyChatAsync(notification.Profile, $"❌ '{searchKeyword}'(으)로 검색되는 치지직 카테고리가 없습니다. 🕵️‍♂️", notification.SenderId, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"🔥 [CategoryStrategy] API 통신 오류: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "⚠️ 치지직 서버와 통신 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요. 🌪️", notification.SenderId, ct);
        }
    }
}
