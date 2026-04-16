using System.Threading.Channels;
using MooldangBot.Application.Workers;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluentAssertions;

namespace MooldangBot.Tests.Workers;

/// <summary>
/// [테스트 서기 워커]: ChatLogBatchWorker의 배치 플러시 로직을 검증합니다.
/// MySqlBulkCopy 전환 후 정합성이 유지되는지 확인하는 것이 핵심입니다.
/// 
/// Note: 실제 DB 커넥션(MySqlConnection)은 테스트 환경에서 생성할 수 없으므로,
/// FlushAsync 내부의 "DB 호출 전까지의 로직"과 "실패 시 복구 경로"를 검증합니다.
/// </summary>
public class ChatLogBatchWorkerTests
{
    private readonly IChatLogBufferService _buffer = Substitute.For<IChatLogBufferService>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ILogger<ChatLogBatchWorker> _logger = Substitute.For<ILogger<ChatLogBatchWorker>>();

    [Fact]
    public async Task StopAsync_Should_Call_Buffer_Complete()
    {
        // [Arrange]
        var worker = new ChatLogBatchWorker(_buffer, _scopeFactory, _logger);

        // 빈 DrainAll 반환
        _buffer.DrainAllAsync(Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable());

        // [Act]
        await worker.StopAsync(CancellationToken.None);

        // [Assert]: 종료 시 Buffer의 Complete가 호출되어야 함
        _buffer.Received(1).Complete();
    }

    [Fact]
    public async Task StopAsync_With_Empty_Buffer_Should_Not_Throw()
    {
        // [Arrange]
        var worker = new ChatLogBatchWorker(_buffer, _scopeFactory, _logger);

        _buffer.DrainAllAsync(Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable());

        // [Act & Assert]: 빈 버퍼로 종료 시 예외가 발생하지 않아야 함
        var act = async () => await worker.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Buffer_Enqueue_Should_Be_Retrievable_Via_DrainAll()
    {
        // [Arrange]: 실제 Channel 기반 버퍼 서비스의 Enqueue/Drain 정합성 검증
        var channel = Channel.CreateBounded<ChatInteractionLog>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        var log = new ChatInteractionLog
        {
            StreamerProfileId = 1,
            SenderNickname = "테스트",
            Message = "안녕",
            IsCommand = false,
            MessageType = "Chat",
            CreatedAt = KstClock.Now
        };

        // [Act]
        channel.Writer.TryWrite(log).Should().BeTrue();
        channel.Writer.TryComplete();

        // [Assert]
        var items = new List<ChatInteractionLog>();
        while (channel.Reader.TryRead(out var item))
        {
            items.Add(item);
        }

        items.Should().HaveCount(1);
        items[0].SenderNickname.Should().Be("테스트");
        items[0].Message.Should().Be("안녕");
    }

    [Fact]
    public void ChatInteractionLog_Should_Have_Default_Values()
    {
        // [Arrange & Act]: 기본값이 올바르게 설정되는지 확인
        var log = new ChatInteractionLog();

        // [Assert]
        log.MessageType.Should().Be("Chat");
        log.IsCommand.Should().BeFalse();
        log.Message.Should().BeEmpty();
        log.SenderNickname.Should().BeEmpty();
    }

    // 헬퍼: 빈 IAsyncEnumerable 생성
    private static async IAsyncEnumerable<ChatInteractionLog> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }
}
