using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Features.Commands.Strategies;

/// <summary>
/// [오시리스의 선포]: 방송 제목(!방제)을 실시간으로 변경하는 전략입니다. (v4.1.0)
/// </summary>
public class TitleStrategy(
    IChzzkApiClient chzzkApi,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    ILogger<TitleStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "Title";

    public async Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        // 1. [정수 추출]: 명령어 키워드 이후의 텍스트를 새로운 방제로 인식
        string msg = notification.Message.Trim();
        string newTitle = msg.Length > command.Keyword.Length ? msg.Substring(command.Keyword.Length).Trim() : "";

        if (string.IsNullOrEmpty(newTitle))
        {
            string statusReply = await dynamicEngine.ProcessMessageAsync("현재 방제: {방제} 🖋️", notification.Profile.ChzzkUid, notification.SenderId);
            await botService.SendReplyChatAsync(notification.Profile, statusReply, notification.SenderId, ct);
            return;
        }

        // 1.1 [정화]: 40자 초과 시 자동 절삭 및 안내 메시지
        if (newTitle.Length > 40)
        {
            newTitle = newTitle[..40];
            await botService.SendReplyChatAsync(notification.Profile, "⚠️ 방송제목은 40자까지만 입력됩니다. (자동 절삭됨) 🖋️", notification.SenderId, ct);
        }

        // 2. [신성한 선언]: 치지직 API를 통해 실제 방제 변경 시도
        try
        {
            logger.LogInformation($"🛠️ [방제 변경 요청] {notification.Username} -> {newTitle}");
            
            var updateData = new { defaultLiveTitle = newTitle };
            bool success = await chzzkApi.UpdateLiveSettingAsync(notification.Profile.ChzzkAccessToken ?? "", updateData);

            if (success)
            {
                logger.LogInformation($"✨ [방제 변경 완료] {notification.Profile.ChzzkUid}: {newTitle}");
                
                // [v4.1.3] DB의 응답 템플릿 사용 (없으면 기본값)
                string responseTemplate = string.IsNullOrEmpty(command.ResponseText) 
                    ? "✅ 방송 제목이 성공적으로 변경되었습니다! {내용} 🖋️" 
                    : command.ResponseText;
                
                // {내용}은 입력값으로 치환하고, 나머지는 엔진에게 맡김
                string processedReply = await dynamicEngine.ProcessMessageAsync(
                    responseTemplate.Replace("{내용}", newTitle), 
                    notification.Profile.ChzzkUid, 
                    notification.SenderId
                );

                await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
            }
            else
            {
                logger.LogWarning($"❌ [방제 변경 실패] {notification.Profile.ChzzkUid} (권한 또는 토큰 만료)");
                await botService.SendReplyChatAsync(notification.Profile, "❌ 방제 변경에 실패했습니다. 스트리머의 권한 설정이나 토큰 상태를 확인해주세요. 🚫", notification.SenderId, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"🔥 [TitleStrategy] API 통신 오류: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "⚠️ 치지직 서버와 통신 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요. 🌪️", notification.SenderId, ct);
        }
    }
}
