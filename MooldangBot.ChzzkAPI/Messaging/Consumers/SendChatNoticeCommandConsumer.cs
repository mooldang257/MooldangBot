using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Chat;
using MooldangBot.Contracts.Chzzk.Models.Commands;

namespace MooldangBot.ChzzkAPI.Messaging.Consumers;

/// <summary>
/// [오시리스의 전령]: 채팅 공지 등록 명령(SendChatNoticeCommand)을 처리하는 소비자입니다.
/// </summary>
public class SendChatNoticeCommandConsumer(
    IChzzkApiClient apiClient,
    IChzzkGatewayTokenStore tokenStore,
    ILogger<SendChatNoticeCommandConsumer> logger) : IConsumer<SendChatNoticeCommand>
{
    public async Task Consume(ConsumeContext<SendChatNoticeCommand> context)
    {
        var command = context.Message;
        logger.LogInformation("🚀 [Consumer] 공지 등록 시작 - Channel: {ChzzkUid}", command.ChzzkUid);

        try
        {
            if (string.IsNullOrEmpty(command.Notice))
            {
                await RespondFailure(context, "공지 내용이 비어있습니다.");
                return;
            }

            var token = await tokenStore.GetTokenAsync(command.ChzzkUid);
            if (string.IsNullOrEmpty(token.AuthCookie))
            {
                await RespondFailure(context, "인증 정보(AuthCookie)가 없습니다.");
                return;
            }

            await apiClient.SetChatNoticeAsync(command.ChzzkUid, new SetChatNoticeRequest { Message = command.Notice }, token.AuthCookie);

            await context.RespondAsync(new StandardCommandResponse(
                CorrelationId: context.CorrelationId ?? Guid.Empty,
                IsSuccess: true,
                ErrorMessage: null,
                ProcessedAt: DateTimeOffset.UtcNow
            ));

            logger.LogInformation("✅ [Consumer] 공지 등록 완료 - Channel: {ChzzkUid}", command.ChzzkUid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Consumer] 공지 등록 중 예외 발생 - Channel: {ChzzkUid}", command.ChzzkUid);
            await RespondFailure(context, ex.Message);
        }
    }

    private static async Task RespondFailure(ConsumeContext<SendChatNoticeCommand> context, string error)
    {
        await context.RespondAsync(new StandardCommandResponse(
            CorrelationId: context.CorrelationId ?? Guid.Empty,
            IsSuccess: false,
            ErrorMessage: error,
            ProcessedAt: DateTimeOffset.UtcNow
        ));
    }
}
