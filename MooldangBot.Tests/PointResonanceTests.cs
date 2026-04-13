using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Features.ChatPoints.Handlers;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Application.Services;
using MooldangBot.Application.Workers;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using NSubstitute;
using Xunit;

namespace MooldangBot.Tests;

public class PointResonanceTests
{
    private readonly IPointBatchService _batchService = new PointBatchService();
    private readonly IPointTransactionService _mockPointService = Substitute.For<IPointTransactionService>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<ChatMessagePointHandler> _handlerLogger = Substitute.For<ILogger<ChatMessagePointHandler>>();
    private readonly ILogger<PointBatchWorker> _workerLogger = Substitute.For<ILogger<PointBatchWorker>>();

    [Fact]
    public async Task PointBatching_Should_Aggregate_And_BulkUpdate()
    {
        // [Arrange]
        var worker = new PointBatchWorker(_batchService, _mockPointService, _workerLogger);
        var streamer = new StreamerProfile { ChzzkUid = "streamer1" };
        var viewerId = "viewer1";

        // [Act]: 10번의 포인트 적립 요청 (배치 서비스로 직접 투입)
        for (int i = 0; i < 10; i++)
        {
            _batchService.EnqueueIncrement(streamer.ChzzkUid, viewerId, "Nickname", 1);
        }

        // 워커의 중단(Flush) 트리거
        await worker.StopAsync(CancellationToken.None);

        // [Assert]: BulkUpdatePointsAsync가 한 번 호출되었고, Total이 10점인지 확인
        await _mockPointService.Received(1).BulkUpdatePointsAsync(
            Arg.Is<IEnumerable<PointJob>>(jobs => jobs.Sum(j => j.Amount) == 10),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ChatMessageHandler_Should_Apply_5s_Cooldown()
    {
        // [Arrange]
        var handler = new ChatMessagePointHandler(_batchService, _cache, _handlerLogger);
        var profile = new StreamerProfile { ChzzkUid = "streamer1" };
        var notification = new ChatMessageReceivedEvent(profile, "User1", "Hello", "common", "viewer1");

        // [Act]: 1초 내에 10번의 채팅 발생
        for (int i = 0; i < 10; i++)
        {
            await handler.Handle(notification, CancellationToken.None);
        }

        // [Assert]: 배치 서비스에는 단 1번만 인큐되어야 함 (쿨다운 때문)
        _batchService.Complete();
        var count = 0;
        await foreach (var _ in _batchService.DrainAllAsync(CancellationToken.None))
        {
            count++;
        }
        
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ChatMessageHandler_With_MockService_Should_Call_Enqueue_Only_Once_Due_To_Cooldown()
    {
        // [Arrange]
        var mockBatch = Substitute.For<IPointBatchService>();
        var handler = new ChatMessagePointHandler(mockBatch, _cache, _handlerLogger);
        var profile = new StreamerProfile { ChzzkUid = "streamer2" };
        var notification = new ChatMessageReceivedEvent(profile, "Spammer", "Spam", "common", "viewer2");

        // [Act]: 연타 채팅
        for (int i = 0; i < 5; i++)
        {
            await handler.Handle(notification, CancellationToken.None);
        }

        // [Assert]: 쿨다운에 의해 EnqueueIncrement는 단 1회만 호출됨
        mockBatch.Received(1).EnqueueIncrement(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
    }
}
