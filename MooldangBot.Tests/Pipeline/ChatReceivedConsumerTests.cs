using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Commands.Events;
using MooldangBot.Contracts.Chzzk.Models.Events;
using MooldangBot.Application.Consumers;
using MooldangBot.Domain.Entities;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MooldangBot.Tests.Pipeline;

/// <summary>
/// [테스트 파수꾼]: MassTransit Consumer → MediatR 파이프라인 통합 테스트입니다.
/// 10k TPS에서 매초 수천 번 호출되는 진입점이므로 정합성이 매우 중요합니다.
/// </summary>
public class ChatReceivedConsumerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IIdentityCacheService _identityCache = Substitute.For<IIdentityCacheService>();
    private readonly ILogger<ChatReceivedConsumer> _logger = Substitute.For<ILogger<ChatReceivedConsumer>>();

    private ChatReceivedConsumer CreateSut() => new(_mediator, _identityCache, _logger);

    private static ChzzkChatEvent CreateChatEvent(string channelId = "streamer1") => new()
    {
        ChannelId = channelId,
        SenderId = "viewer1",
        Nickname = "TestUser",
        Content = "안녕하세요!",
        Timestamp = DateTime.UtcNow
    };

    [Fact]
    public async Task Consume_ValidChatEvent_Should_Publish_MediatR_Notification()
    {
        // [Arrange]
        var consumer = CreateSut();
        var chatEvent = CreateChatEvent();
        var profile = new StreamerProfile { Id = 1, ChzzkUid = "streamer1", ChannelName = "테스트채널" };

        _identityCache.GetStreamerProfileAsync("streamer1", Arg.Any<CancellationToken>())
            .Returns(profile);

        var context = Substitute.For<ConsumeContext<ChzzkChatEvent>>();
        context.Message.Returns(chatEvent);
        context.CancellationToken.Returns(CancellationToken.None);
        context.CorrelationId.Returns(Guid.NewGuid());

        // [Act]
        await consumer.Consume(context);

        // [Assert]: MediatR Publish가 정확히 1회 호출되어야 함
        await _mediator.Received(1).Publish(
            Arg.Is<ChzzkEventReceived>(e =>
                e.Profile.ChzzkUid == "streamer1" &&
                e.Payload is ChzzkChatEvent),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_UnknownStreamer_Should_Skip_Without_Publishing()
    {
        // [Arrange]
        var consumer = CreateSut();
        var chatEvent = CreateChatEvent("unknown_streamer");

        // 스트리머를 찾을 수 없는 경우
        _identityCache.GetStreamerProfileAsync("unknown_streamer", Arg.Any<CancellationToken>())
            .Returns((StreamerProfile?)null);

        var context = Substitute.For<ConsumeContext<ChzzkChatEvent>>();
        context.Message.Returns(chatEvent);
        context.CancellationToken.Returns(CancellationToken.None);

        // [Act]
        await consumer.Consume(context);

        // [Assert]: MediatR Publish가 호출되지 않아야 함
        await _mediator.DidNotReceive().Publish(
            Arg.Any<ChzzkEventReceived>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_IdentityCacheThrows_Should_Not_Propagate_Exception()
    {
        // [Arrange]
        var consumer = CreateSut();
        var chatEvent = CreateChatEvent();

        _identityCache.GetStreamerProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Redis 연결 실패"));

        var context = Substitute.For<ConsumeContext<ChzzkChatEvent>>();
        context.Message.Returns(chatEvent);
        context.CancellationToken.Returns(CancellationToken.None);
        context.MessageId.Returns(Guid.NewGuid());

        // [Act & Assert]: 예외가 Consumer 내부에서 try-catch로 격리되므로 외부로 전파되지 않아야 함
        // ChatReceivedConsumer는 내부적으로 catch하고 로깅만 합니다
        await consumer.Consume(context);

        // MediatR Publish가 호출되지 않아야 함
        await _mediator.DidNotReceive().Publish(
            Arg.Any<ChzzkEventReceived>(),
            Arg.Any<CancellationToken>());
    }
}
