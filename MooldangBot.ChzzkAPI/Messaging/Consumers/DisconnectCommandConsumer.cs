using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Contracts.Chzzk.Models.Commands;

namespace MooldangBot.ChzzkAPI.Messaging.Consumers;

/// <summary>
/// [오시리스의 전령]: 채널 세션 연결 해제 명령(DisconnectCommand)을 처리하는 소비자입니다.
/// </summary>
public class DisconnectCommandConsumer(
    IShardedWebSocketManager shardManager,
    ILogger<DisconnectCommandConsumer> logger) : IConsumer<DisconnectCommand>
{
    public async Task Consume(ConsumeContext<DisconnectCommand> context)
    {
        var command = context.Message;
        logger.LogInformation("🚀 [Consumer] 세션 연결 해제 시작 - Channel: {ChzzkUid}", command.ChzzkUid);

        try
        {
            await shardManager.DisconnectAsync(command.ChzzkUid);

            await context.RespondAsync(new StandardCommandResponse(
                CorrelationId: context.CorrelationId ?? Guid.Empty,
                IsSuccess: true,
                ErrorMessage: null,
                ProcessedAt: DateTimeOffset.UtcNow
            ));

            logger.LogInformation("✅ [Consumer] 세션 연결 해제 완료 - Channel: {ChzzkUid}", command.ChzzkUid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Consumer] 세션 연결 해제 중 예외 발생 - Channel: {ChzzkUid}", command.ChzzkUid);
            await RespondFailure(context, ex.Message);
        }
    }

    private static async Task RespondFailure(ConsumeContext<DisconnectCommand> context, string error)
    {
        await context.RespondAsync(new StandardCommandResponse(
            CorrelationId: context.CorrelationId ?? Guid.Empty,
            IsSuccess: false,
            ErrorMessage: error,
            ProcessedAt: DateTimeOffset.UtcNow
        ));
    }
}
