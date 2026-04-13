using System.Threading.Channels;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Services;

/// <summary>
/// [오시리스의 서판 구현체]: 고성능 채널(Channel)을 사용하여 채팅 로그를 비차단 방식으로 수집합니다.
/// </summary>
public class ChatLogBufferService : IChatLogBufferService
{
    // [v1.0] 10k RPS 급 폭주 시 약 5초간의 비상 완충 공간 확보 (50,000건)
    private readonly Channel<ChatInteractionLog> _channel = Channel.CreateBounded<ChatInteractionLog>(new BoundedChannelOptions(50000)
    {
        FullMode = BoundedChannelFullMode.Wait, // 큐가 꽉 차면 생산자 속도를 늦춰 시스템 전체 붕괴 방지
        SingleReader = true,  // 전 전용 워커(ChatLogBatchWorker)만 읽음
        SingleWriter = false  // 다수의 채팅 핸들러가 동시에 씀
    });

    public void Enqueue(ChatInteractionLog log)
    {
        // TryWrite는 비차단으로 즉시 반환됨 (성능 최우선)
        if (!_channel.Writer.TryWrite(log))
        {
            // 사실상 5만 건이 찰 정도면 시스템 비상 상황
        }
    }

    public IAsyncEnumerable<ChatInteractionLog> DrainAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}
