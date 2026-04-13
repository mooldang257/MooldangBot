using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Live;
using MooldangBot.Contracts.Chzzk.Models.Commands;

namespace MooldangBot.ChzzkAPI.Messaging.Consumers;

/// <summary>
/// [오시리스의 전령]: 방송 카테고리 변경 명령(UpdateCategoryCommand)을 처리하는 소비자입니다.
/// </summary>
public class UpdateCategoryCommandConsumer(
    IChzzkApiClient apiClient,
    IChzzkGatewayTokenStore tokenStore,
    ILogger<UpdateCategoryCommandConsumer> logger) : IConsumer<UpdateCategoryCommand>
{
    public async Task Consume(ConsumeContext<UpdateCategoryCommand> context)
    {
        var command = context.Message;
        logger.LogInformation("🚀 [Consumer] 카테고리 변경 시작 - Channel: {ChzzkUid}, ID: {Id}, Keyword: {Keyword}", 
            command.ChzzkUid, command.CategoryId, command.SearchKeyword);

        try
        {
            var token = await tokenStore.GetTokenAsync(command.ChzzkUid);
            if (string.IsNullOrEmpty(token.AuthCookie))
            {
                await RespondFailure(context, "인증 정보(AuthCookie)가 없습니다.");
                return;
            }

            var categoryId = command.CategoryId;
            var categoryType = command.CategoryType;

            // [v3.1.6] 카테고리 ID가 없으면 게이트웨이에서 직접 수색
            if (string.IsNullOrEmpty(categoryId) && !string.IsNullOrEmpty(command.SearchKeyword))
            {
                logger.LogInformation("🔍 [카테고리 수색] Keyword: {Keyword}", command.SearchKeyword);
                var searchRes = await apiClient.SearchCategoryAsync(command.SearchKeyword);
                var firstResult = searchRes?.Data?.FirstOrDefault();
                
                if (firstResult != null)
                {
                    categoryId = firstResult.CategoryId;
                    categoryType = firstResult.CategoryType;
                    logger.LogInformation("🎯 [수색 성공] 발견: {Value} ({Id})", firstResult.CategoryValue, categoryId);
                }
                else
                {
                    await RespondFailure(context, $"'{command.SearchKeyword}'에 해당하는 카테고리를 찾지 못했습니다.");
                    return;
                }
            }

            if (string.IsNullOrEmpty(categoryId))
            {
                await RespondFailure(context, "카테고리 ID를 확정할 수 없습니다.");
                return;
            }

            await apiClient.UpdateLiveSettingAsync(command.ChzzkUid, new UpdateLiveSettingRequest 
            { 
                CategoryId = categoryId,
                CategoryType = categoryType
            }, token.AuthCookie);

            await context.RespondAsync(new StandardCommandResponse(
                CorrelationId: context.CorrelationId ?? Guid.Empty,
                IsSuccess: true,
                ErrorMessage: null,
                ProcessedAt: DateTimeOffset.UtcNow
            ));

            logger.LogInformation("✅ [Consumer] 카테고리 변경 완료 - Channel: {ChzzkUid}", command.ChzzkUid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Consumer] 카테고리 변경 중 예외 발생 - Channel: {ChzzkUid}", command.ChzzkUid);
            await RespondFailure(context, ex.Message);
        }
    }

    private static async Task RespondFailure(ConsumeContext<UpdateCategoryCommand> context, string error)
    {
        await context.RespondAsync(new StandardCommandResponse(
            CorrelationId: context.CorrelationId ?? Guid.Empty,
            IsSuccess: false,
            ErrorMessage: error,
            ProcessedAt: DateTimeOffset.UtcNow
        ));
    }
}
