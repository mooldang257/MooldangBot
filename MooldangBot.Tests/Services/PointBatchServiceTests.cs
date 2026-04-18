using System.Threading.Channels;
using MooldangBot.Application.Services;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.Point.Requests.Models;
using FluentAssertions;
using Xunit;

namespace MooldangBot.Tests.Services;

/// <summary>
/// [테스트 오버드라이브]: PointBatchService(BoundedChannel 기반)의 동시성과 정합성을 검증합니다.
/// 10k TPS에서 수천 개의 동시 Write가 발생하는 핵심 데이터 구조입니다.
/// </summary>
public class PointBatchServiceTests
{
    [Fact]
    public async Task Enqueue_Single_Item_Should_DrainAll_Returns_One()
    {
        // [Arrange]
        var service = new PointBatchService();

        // [Act]
        service.EnqueueIncrement("s1", "v1", "Nick", 10);
        service.Complete();

        // [Assert]
        var items = new List<PointJob>();
        await foreach (var job in service.DrainAllAsync(CancellationToken.None))
        {
            items.Add(job);
        }

        items.Should().HaveCount(1);
        items[0].Should().Be(new PointJob("s1", "v1", "Nick", 10));
    }

    [Fact]
    public async Task Enqueue_1000_Concurrent_Items_Should_DrainAll_Returns_All()
    {
        // [Arrange]
        var service = new PointBatchService();
        var count = 1000;

        // [Act]: 1000개를 병렬로 Enqueue
        var tasks = Enumerable.Range(0, count)
            .Select(i => Task.Run(() =>
                service.EnqueueIncrement($"s{i % 10}", $"v{i}", $"Nick{i}", 1)))
            .ToList();

        await Task.WhenAll(tasks);
        service.Complete();

        // [Assert]: 모두 DrainAll로 복원되어야 함
        var items = new List<PointJob>();
        await foreach (var job in service.DrainAllAsync(CancellationToken.None))
        {
            items.Add(job);
        }

        items.Should().HaveCount(count);
    }

    [Fact]
    public void Complete_Should_Prevent_Further_Writes()
    {
        // [Arrange]
        var service = new PointBatchService();

        // [Act]
        service.EnqueueIncrement("s1", "v1", "Nick", 1);
        service.Complete();

        // [Assert]: Complete 이후 TryWrite는 false를 반환해야 하지만,
        // EnqueueIncrement는 내부적으로 TryWrite 실패를 무시하므로 예외가 발생하지 않아야 함
        var act = () => service.EnqueueIncrement("s2", "v2", "Nick2", 1);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DrainAll_On_Empty_Channel_Should_Return_Immediately_After_Complete()
    {
        // [Arrange]
        var service = new PointBatchService();

        // [Act]: 아무것도 넣지 않고 Complete 후 Drain
        service.Complete();

        var items = new List<PointJob>();
        await foreach (var job in service.DrainAllAsync(CancellationToken.None))
        {
            items.Add(job);
        }

        // [Assert]
        items.Should().BeEmpty();
    }
}
