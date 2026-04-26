using MooldangBot.Application.Features.ChatPoints.Handlers;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using MooldangBot.Domain.Entities;
using MooldangBot.Modules.Point.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MooldangBot.Tests.Handlers;

public class ChatMessagePointHandlerTests
{
    private readonly IPointCacheService _cacheService = Substitute.For<IPointCacheService>();
    private readonly ILogger<ChatMessagePointHandler> _logger = Substitute.For<ILogger<ChatMessagePointHandler>>();

    private ChatMessagePointHandler CreateSut() => new(_cacheService, _logger);

    private static StreamerProfile CreateProfile() => new()
    {
        Id = 1,
        ChzzkUid = "streamer1",
        ChannelName = "테스트"
    };

    [Fact]
    public async Task Handle_ChatEvent_Should_Enqueue_Point_Increment()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var chatEvent = new ChzzkChatEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer123",
            Nickname = "물댕이",
            Content = "안녕하세요!",
            Timestamp = DateTime.UtcNow
        };

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, chatEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]: AddPointAsync가 정확한 파라미터로 1회 호출되어야 함
        await _cacheService.Received(1).AddPointAsync("streamer1", "viewer123", "물댕이", 1);
    }

    [Fact]
    public async Task Handle_DonationEvent_Should_Skip()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var donationEvent = new ChzzkDonationEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer1",
            Nickname = "후원자",
            DonationMessage = "응원해요!",
            PayAmount = 5000,
            Timestamp = DateTime.UtcNow
        };

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, donationEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]: 후원 이벤트는 포인트 적립 대상이 아님
        await _cacheService.DidNotReceive().AddPointAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Handle_ChatEvent_Without_SenderId_Should_Skip()
    {
        // [Arrange]
        var handler = CreateSut();
        var profile = CreateProfile();
        var chatEvent = new ChzzkChatEvent
        {
            ChannelId = "streamer1",
            SenderId = "", // 빈 SenderId (시스템 메시지 등)
            Nickname = "System",
            Content = "시스템 메시지",
            Timestamp = DateTime.UtcNow
        };

        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, chatEvent, DateTimeOffset.UtcNow);

        // [Act]
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]: SenderId가 없으면 포인트 적립 안 함
        await _cacheService.DidNotReceive().AddPointAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
    }
}
