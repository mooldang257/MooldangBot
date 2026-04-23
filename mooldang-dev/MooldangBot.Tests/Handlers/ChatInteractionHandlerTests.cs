using MooldangBot.Application.Features.Chat.Handlers;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using MooldangBot.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluentAssertions;

namespace MooldangBot.Tests.Handlers;

/// <summary>
/// [테스트 서기]: ChatInteractionHandler의 로그 기록 정합성을 검증합니다.
/// 이 핸들러는 모든 채팅/후원 메시지를 감사(Audit)하여 벌크 버퍼에 기록하는 10k TPS 핫패스입니다.
/// </summary>
public class ChatInteractionHandlerTests
{
    private readonly IChatLogBufferService _buffer = Substitute.For<IChatLogBufferService>();
    private readonly ICommandCache _commandCache = Substitute.For<ICommandCache>();
    private readonly IBroadcastScribe _scribe = Substitute.For<IBroadcastScribe>();
    private readonly ILogger<ChatInteractionHandler> _logger = Substitute.For<ILogger<ChatInteractionHandler>>();

    private ChatInteractionHandler CreateSut() => new(_buffer, _commandCache, _scribe, _logger);

    private static StreamerProfile CreateProfile() => new()
    {
        Id = 1,
        ChzzkUid = "streamer1",
        ChannelName = "테스트"
    };

    [Fact]
    public async Task Handle_ChatEvent_Should_Enqueue_With_Correct_Fields()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var chatEvent = new ChzzkChatEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer1",
            Nickname = "물댕이",
            Content = "안녕하세요!",
            Timestamp = DateTime.UtcNow
        };

        _commandCache.GetMatchesAsync("streamer1", "안녕하세요!")
            .Returns(Enumerable.Empty<MooldangBot.Domain.DTOs.CommandMetadata>());

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, chatEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]
        _buffer.Received(1).Enqueue(Arg.Is<ChatInteractionLog>(log =>
            log.StreamerProfileId == 1 &&
            log.SenderNickname == "물댕이" &&
            log.Message == "안녕하세요!" &&
            log.MessageType == "Chat" &&
            log.IsCommand == false));

        _scribe.Received(1).AddChatMessage("streamer1", "안녕하세요!");
    }

    [Fact]
    public async Task Handle_DonationEvent_Should_Set_MessageType_Donation()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var donationEvent = new ChzzkDonationEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer1",
            Nickname = "후원자",
            DonationMessage = "응원합니다!",
            PayAmount = 10000,
            Timestamp = DateTime.UtcNow
        };

        _commandCache.GetMatchesAsync("streamer1", "응원합니다!")
            .Returns(Enumerable.Empty<MooldangBot.Domain.DTOs.CommandMetadata>());

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, donationEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]
        _buffer.Received(1).Enqueue(Arg.Is<ChatInteractionLog>(log =>
            log.MessageType == "Donation" &&
            log.SenderNickname == "후원자"));
    }

    [Fact]
    public async Task Handle_EmptyMessage_Should_Skip()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var chatEvent = new ChzzkChatEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer1",
            Nickname = "물댕이",
            Content = "", // 빈 메시지
            Timestamp = DateTime.UtcNow
        };

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, chatEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]: Enqueue가 호출되지 않아야 함
        _buffer.DidNotReceive().Enqueue(Arg.Any<ChatInteractionLog>());
    }

    [Fact]
    public async Task Handle_SubscriptionEvent_Should_Skip()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var subEvent = new ChzzkSubscriptionEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer1",
            Nickname = "구독자",
            SubscriptionTier = 1,
            SubscriptionMonth = 3,
            Timestamp = DateTime.UtcNow
        };

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, subEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]: 구독 이벤트는 무시해야 함
        _buffer.DidNotReceive().Enqueue(Arg.Any<ChatInteractionLog>());
    }

    [Fact]
    public async Task Handle_CommandMessage_Should_Set_IsCommand_True()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var chatEvent = new ChzzkChatEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer1",
            Nickname = "물댕이",
            Content = "!룰렛",
            Timestamp = DateTime.UtcNow
        };

        // 명령어 캐시가 매칭 결과를 반환
        var matchedCommand = new MooldangBot.Domain.DTOs.CommandMetadata
        {
            Id = 1,
            Keyword = "!룰렛",
            FeatureType = MooldangBot.Domain.Entities.CommandFeatureType.Roulette,
            StreamerProfileId = 1
        };
        _commandCache.GetMatchesAsync("streamer1", "!룰렛")
            .Returns(new[] { matchedCommand });

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, chatEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]: IsCommand가 true로 설정되어야 함
        _buffer.Received(1).Enqueue(Arg.Is<ChatInteractionLog>(log =>
            log.IsCommand == true &&
            log.Message == "!룰렛"));
    }
}
