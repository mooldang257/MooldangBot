using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Live;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.ChzzkAPI.Messaging.Consumers;

/// <summary>
/// [오시리스의 전령]: 방송 제목 변경 명령(UpdateTitleCommand)을 처리하는 소비자입니다.
/// </summary>
public class UpdateTitleCommandConsumer(
    IChzzkApiClient apiClient,
    IChzzkGatewayTokenStore tokenStore,
    ILogger<UpdateTitleCommandConsumer> logger) : IConsumer<UpdateTitleCommand>
{
    public async Task Consume(ConsumeContext<UpdateTitleCommand> context)
    {
        var command = context.Message;
        logger.LogInformation("🚀 [Consumer] 제목 변경 시작 - Channel: {ChzzkUid}, Title: {Title}", command.ChzzkUid, command.NewTitle);

        try
        {
            if (string.IsNullOrEmpty(command.NewTitle))
            {
                await RespondFailure(context, "제목 내용이 비어있습니다.");
                return;
            }

            var token = await tokenStore.GetTokenAsync(command.ChzzkUid);
            if (string.IsNullOrEmpty(token.AuthCookie))
            {
                await RespondFailure(context, "인증 정보(AuthCookie)가 없습니다.");
                return;
            }

            await apiClient.UpdateLiveSettingAsync(command.ChzzkUid, new UpdateLiveSettingRequest { DefaultLiveTitle = command.NewTitle }, token.AuthCookie);

            await context.RespondAsync(new StandardCommandResponse(
                CorrelationId: context.CorrelationId ?? Guid.Empty,
                IsSuccess: true,
                ErrorMessage: null,
                ProcessedAt: DateTimeOffset.UtcNow
            ));

            logger.LogInformation("✅ [Consumer] 제목 변경 완료 - Channel: {ChzzkUid}", command.ChzzkUid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Consumer] 제목 변경 중 예외 발생 - Channel: {ChzzkUid}", command.ChzzkUid);
            await RespondFailure(context, ex.Message);
        }
    }

    private static async Task RespondFailure(ConsumeContext<UpdateTitleCommand> context, string error)
    {
        await context.RespondAsync(new StandardCommandResponse(
            CorrelationId: context.CorrelationId ?? Guid.Empty,
            IsSuccess: false,
            ErrorMessage: error,
            ProcessedAt: DateTimeOffset.UtcNow
        ));
    }
}
