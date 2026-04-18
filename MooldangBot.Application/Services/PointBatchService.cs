using System.Threading.Channels;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.Point.Requests.Models;

namespace MooldangBot.Application.Services;

/// <summary>
/// [오버드라이브 엔진]: 초고속 채팅 포인트 수집을 위한 Channel 기반 서비스입니다.
/// (P1: 성능): 락(Lock) 없이 비동기 큐를 사용하여 수천 개의 채팅을 비차단으로 수용합니다.
/// </summary>
public class PointBatchService : IPointBatchService
{
    // BoundedChannel을 사용하여 메모리 폭주 방지 (10k RPS 폭주 대응: 최대 10만 건 대기 가능)
    private readonly Channel<PointJob> _channel = Channel.CreateBounded<PointJob>(new BoundedChannelOptions(100000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true, // PointBatchWorker만 읽음
        SingleWriter = false // 다수의 채팅 핸들러가 씀
    });

    public void EnqueueIncrement(string streamerUid, string viewerUid, string nickname, int amount)
    {
        // TryWrite는 비차단(Non-blocking)으로 즉시 반환됨 (성능 최우선)
        if (!_channel.Writer.TryWrite(new PointJob(streamerUid, viewerUid, nickname, amount)))
        {
            // 큐가 가득 찼을 경우에만 여기서 예외를 처리하거나 대기할 수 있음 (현재는 성능을 위해 무시 가능)
        }
    }

    public IAsyncEnumerable<PointJob> DrainAllAsync(CancellationToken ct)
    {
        // 채널의 모든 데이터를 비동기적으로 스트리밍
        return _channel.Reader.ReadAllAsync(ct);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}
