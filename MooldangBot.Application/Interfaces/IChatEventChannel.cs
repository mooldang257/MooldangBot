using System.Threading.Channels;
using MooldangBot.Application.Models.Chat;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 Bridge]: 수집 레이어와 처리 레이어 사이를 연결하는 고속 채널 인터페이스입니다.
/// </summary>
public interface IChatEventChannel
{
    /// <summary>
    /// 패킷을 채널에 기록합니다.
    /// </summary>
    bool TryWrite(ChatEventPacket packet);

    /// <summary>
    /// 채널에서 모든 패킷을 비동기적으로 읽어옵니다.
    /// </summary>
    IAsyncEnumerable<ChatEventPacket> ReadAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 현재 대기 중인 패킷 수입니다.
    /// </summary>
    int PendingCount { get; }
}
