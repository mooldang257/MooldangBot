using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Commands;

namespace MooldangBot.ChzzkAPI.Messaging.Consumers;

/// <summary>
/// [오시리스의 전령]: 채널 세션 재연결 명령(ReconnectCommand)을 처리하는 소비자입니다.
/// </summary>
public class ReconnectCommandConsumer(
    IShardedWebSocketManager shardManager,
    IChzzkApiClient apiClient,
    IChzzkGatewayTokenStore tokenStore,
    ILogger<ReconnectCommandConsumer> logger) : IConsumer<ReconnectCommand>
{
    public async Task Consume(ConsumeContext<ReconnectCommand> context)
    {
        var command = context.Message;
        logger.LogInformation("🚀 [Consumer] 세션 재연결 시작 - Channel: {ChzzkUid}", command.ChzzkUid);

        try
        {
            var token = await tokenStore.GetTokenAsync(command.ChzzkUid);
            if (string.IsNullOrEmpty(token.AuthCookie))
            {
                await RespondFailure(context, "인증 정보(AuthCookie)가 없습니다.");
                return;
            }

            var sessionResponse = await apiClient.GetSessionUrlAsync(command.ChzzkUid, token.AuthCookie);
            if (sessionResponse != null && !string.IsNullOrEmpty(sessionResponse.Url))
            {
                await shardManager.ConnectAsync(command.ChzzkUid, sessionResponse.Url, token.AuthCookie);
                
                await context.RespondAsync(new StandardCommandResponse(
                    CorrelationId: context.CorrelationId ?? Guid.Empty,
                    IsSuccess: true,
                    ErrorMessage: null,
                    ProcessedAt: DateTimeOffset.UtcNow
                ));

                logger.LogInformation("✅ [Consumer] 세션 재연결 요청 완료 - Channel: {ChzzkUid}", command.ChzzkUid);
            }
            else
            {
                await RespondFailure(context, "세션 URL 획득 실패");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Consumer] 세션 재연결 중 예외 발생 - Channel: {ChzzkUid}", command.ChzzkUid);
            await RespondFailure(context, ex.Message);
        }
    }

    private static async Task RespondFailure(ConsumeContext<ReconnectCommand> context, string error)
    {
        await context.RespondAsync(new StandardCommandResponse(
            CorrelationId: context.CorrelationId ?? Guid.Empty,
            IsSuccess: false,
            ErrorMessage: error,
            ProcessedAt: DateTimeOffset.UtcNow
        ));
    }
}
