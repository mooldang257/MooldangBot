using MooldangBot.Contracts.Interfaces;
using MooldangBot.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Events;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Events;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Consumers;

/// <summary>
/// [오시리스의 영혼]: MassTransit을 통해 치지직 채팅 이벤트를 수신하는 고성능 소비자입니다.
/// (v4.0): 기존의 브릿지(ChatEventChannel)를 거치지 않고 MediatR 파이프라인으로 직접 전달합니다.
/// </summary>
public sealed class ChatReceivedConsumer(
    IMediator mediatr,
    IIdentityCacheService identityCache,
    ILogger<ChatReceivedConsumer> logger) : IConsumer<ChzzkChatEvent>
{
    public async Task Consume(ConsumeContext<ChzzkChatEvent> context)
    {
        var chatEvent = context.Message;

        try
        {
            // 1. [파로스의 자각]: 캐시에서 스트리머 프로필을 즉시 확인 (Singleton 캐시 활용)
            var profile = await identityCache.GetStreamerProfileAsync(chatEvent.ChannelId);
            if (profile == null)
            {
                logger.LogWarning("⚠️ [ChatConsumer] 프로필을 찾을 수 없습니다: {ChzzkUid}", chatEvent.ChannelId);
                return;
            }

            // 2. [오시리스의 위임]: 레거시 정합성을 유지하며 MediatR 파이프라인으로 전파
            // (Note: 추후 10k TPS 임계치 도달 시 MediatR을 바이패스하고 도메인 서비스를 직접 호출하도록 확장 가능)
            var internalEvent = new ChzzkEventReceived(
                context.CorrelationId ?? Guid.NewGuid(),
                profile,
                chatEvent,
                chatEvent.Timestamp
            );

            await mediatr.Publish(internalEvent, context.CancellationToken);

            logger.LogDebug("✅ [ChatConsumer] 메시지 처리 위임 완료: {MsgId} (Sender: {Nickname})", 
                context.CorrelationId, chatEvent.Nickname);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [ChatConsumer] 메시지 소비 중 오류 발생: {MessageId}", context.MessageId);
        }
    }
}
