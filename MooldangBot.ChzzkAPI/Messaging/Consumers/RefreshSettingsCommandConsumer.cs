using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Integrations.Chzzk.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.ChzzkAPI.Messaging.Consumers;

/// <summary>
/// [오시리스의 전령]: 채널 설정 새로고침(RefreshSettingsCommand) 및 재연결 명령을 처리하는 소비자입니다.
/// </summary>
public class RefreshSettingsCommandConsumer(
    IShardedWebSocketManager shardManager,
    IChzzkApiClient apiClient,
    IChzzkGatewayTokenStore tokenStore,
    ILogger<RefreshSettingsCommandConsumer> logger) : IConsumer<RefreshSettingsCommand>
{
    public async Task Consume(ConsumeContext<RefreshSettingsCommand> context)
    {
        var command = context.Message;
        logger.LogWarning("🚨 [Consumer] 자가 치유 시작 (설정 새로고침) - Channel: {ChzzkUid}", command.ChzzkUid);

        try
        {
            // 1. 기존 연결 해제
            await shardManager.DisconnectAsync(command.ChzzkUid);

            // 2. 재연결 시도 (토큰 및 세션 URL 갱신)
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

                logger.LogInformation("✅ [Consumer] 자가 치유(재연결) 완료 - Channel: {ChzzkUid}", command.ChzzkUid);
            }
            else
            {
                await RespondFailure(context, "세션 URL 획득 실패");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Consumer] 자가 치유 중 예외 발생 - Channel: {ChzzkUid}", command.ChzzkUid);
            await RespondFailure(context, ex.Message);
        }
    }

    private static async Task RespondFailure(ConsumeContext<RefreshSettingsCommand> context, string error)
    {
        await context.RespondAsync(new StandardCommandResponse(
            CorrelationId: context.CorrelationId ?? Guid.Empty,
            IsSuccess: false,
            ErrorMessage: error,
            ProcessedAt: DateTimeOffset.UtcNow
        ));
    }
}
