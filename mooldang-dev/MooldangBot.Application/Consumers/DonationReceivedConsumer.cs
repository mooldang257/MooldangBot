using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;

namespace MooldangBot.Application.Consumers;

/// <summary>
/// [오시리스의 영혼]: MassTransit을 통해 치지직 후원 이벤트를 수신하는 소비자입니다.
/// </summary>
public sealed class DonationReceivedConsumer(
    IMediator mediatr,
    IIdentityCacheService identityCache,
    ILogger<DonationReceivedConsumer> logger) : IConsumer<ChzzkDonationEvent>
{
    public async Task Consume(ConsumeContext<ChzzkDonationEvent> context)
    {
        var donationEvent = context.Message;
        logger.LogInformation("💰 [DonationConsumer] Starting to consume donation: {Amount} from {User} (Msg: {Msg})", donationEvent.PayAmount, donationEvent.Nickname, donationEvent.DonationMessage);

        try
        {
            logger.LogDebug("[DonationConsumer] Fetching profile for ChannelId: {ChannelId}", donationEvent.ChannelId);
            var profile = await identityCache.GetStreamerProfileAsync(donationEvent.ChannelId);
            
            if (profile == null)
            {
                logger.LogWarning("⚠️ [DonationConsumer] Profile not found for ChannelId: {ChannelId}", donationEvent.ChannelId);
                return;
            }

            logger.LogInformation("💰 [DonationConsumer] Profile found: {ProfileName}. Publishing internal event.", profile.ChannelName);

            var internalEvent = new ChzzkEventReceived(
                context.CorrelationId ?? Guid.NewGuid(),
                profile,
                donationEvent,
                donationEvent.Timestamp
            );

            await mediatr.Publish(internalEvent, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [DonationConsumer] 메시지 소비 중 오류 발생: {MessageId}", context.MessageId);
        }
    }
}
