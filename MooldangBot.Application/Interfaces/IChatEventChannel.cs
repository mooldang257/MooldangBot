using System.Threading.Channels;
using MooldangBot.Application.Models;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [Phase1: 역압 처리] 채팅 이벤트를 큐잉하고 소비하는 Bounded Channel 인터페이스입니다.
/// </summary>
public interface IChatEventChannel
{
    /// <summary>
    /// 이벤트를 큐에 비동기적으로 기록합니다. 큐가 가득 차면 가장 오래된 항목이 드롭됩니다.
    /// </summary>
    bool TryWrite(ChatEventItem item);

    /// <summary>
    /// 큐에서 이벤트를 비동기적으로 읽어옵니다.
    /// </summary>
    IAsyncEnumerable<ChatEventItem> ReadAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 현재 큐에 대기 중인 이벤트 수를 반환합니다.
    /// </summary>
    int PendingCount { get; }
}
