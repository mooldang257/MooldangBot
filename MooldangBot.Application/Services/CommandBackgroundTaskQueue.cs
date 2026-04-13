using System.Threading.Channels;

namespace MooldangBot.Application.Services;

/// <summary>
/// [세피로스의 흐름]: 고부하 또는 지연이 예상되는 작업을 백그라운드로 오프로딩하는 채널 기반 큐입니다.
/// </summary>
public interface ICommandBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}

public class CommandBackgroundTaskQueue : ICommandBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public CommandBackgroundTaskQueue()
    {
        // 유계 채널(Bounded Channel)로 설계하여 메모리 폭주 방지 (Capacity: 1000)
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
