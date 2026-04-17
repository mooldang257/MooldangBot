using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Features.ChatPoints.Handlers;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Commands.Events;
using MooldangBot.Contracts.Chzzk.Models.Events;
using MooldangBot.Application.Services;
using MooldangBot.Infrastructure.Workers.Points;
using MooldangBot.Domain.Entities;
using MooldangBot.Contracts.Point.Requests.Models;
using MooldangBot.Contracts.Common.Services;
using NSubstitute;
using Xunit;

namespace MooldangBot.Tests;

/// <summary>
/// [포인트 공명 테스트]: 포인트 적립 핸들러 → 배치 서비스 → 배치 워커 간의 데이터 흐름 정합성을 검증합니다.
/// (v2.0) ChatMessageReceivedEvent_Legacy → ChzzkEventReceived 전환 반영
/// (v2.1) PointBatchWorker 생성자 시그니처 업데이트 (5개 파라미터)
/// </summary>
public class PointResonanceTests
{
    private readonly IPointBatchService _batchService = new PointBatchService();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly PulseService _pulse = new(Substitute.For<StackExchange.Redis.IConnectionMultiplexer>(), Substitute.For<ILogger<PulseService>>());
    private readonly ChaosManager _chaos = new(Substitute.For<ILogger<ChaosManager>>());
    private readonly ILogger<ChatMessagePointHandler> _handlerLogger = Substitute.For<ILogger<ChatMessagePointHandler>>();
    private readonly ILogger<PointBatchWorker> _workerLogger = Substitute.For<ILogger<PointBatchWorker>>();

    [Fact]
    public async Task Handler_Should_Enqueue_Point_For_Valid_Chat()
    {
        // [Arrange]: 실제 배치 서비스를 사용하여 핸들러 → 서비스 흐름 검증
        var handler = new ChatMessagePointHandler(_batchService, _handlerLogger);
        var profile = new StreamerProfile { ChzzkUid = "streamer1", ChannelName = "Test" };
        var chatEvent = new ChzzkChatEvent
        {
            ChannelId = "streamer1",
            SenderId = "viewer1",
            Nickname = "User1",
            Content = "Hello",
            Timestamp = DateTime.UtcNow
        };
        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, chatEvent, DateTimeOffset.UtcNow);

        // [Act]: 채팅 이벤트 처리
        await handler.Handle(notification, CancellationToken.None);

        // [Assert]: BatchService에 1건이 적재되었는지 확인
        _batchService.Complete();
        var count = 0;
        await foreach (var _ in _batchService.DrainAllAsync(CancellationToken.None))
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handler_With_MockService_Should_Call_Enqueue()
    {
        // [Arrange]
        var mockBatch = Substitute.For<IPointBatchService>();
        var handler = new ChatMessagePointHandler(mockBatch, _handlerLogger);
        var profile = new StreamerProfile { ChzzkUid = "streamer2", ChannelName = "Test" };
        var chatEvent = new ChzzkChatEvent
        {
            ChannelId = "streamer2",
            SenderId = "viewer2",
            Nickname = "Spammer",
            Content = "Spam",
            Timestamp = DateTime.UtcNow
        };
        var notification = new ChzzkEventReceived(Guid.NewGuid(), profile, chatEvent, DateTimeOffset.UtcNow);

        // [Act]: 연타 채팅 (현재 핸들러에는 쿨다운이 제거되었으므로 모두 통과)
        for (int i = 0; i < 5; i++)
        {
            await handler.Handle(notification, CancellationToken.None);
        }

        // [Assert]: 쿨다운 없이 5회 모두 EnqueueIncrement 호출
        mockBatch.Received(5).EnqueueIncrement(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
    }
}
