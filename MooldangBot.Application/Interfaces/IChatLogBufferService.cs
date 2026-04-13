using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 서판]: 초고속으로 유입되는 채팅 로그를 메인 DB 쓰기 부하 없이 수집하기 위한 하이-스로풋 버퍼 인터페이스입니다.
/// </summary>
public interface IChatLogBufferService
{
    /// <summary>
    /// 로그를 큐에 즉시 삽입합니다. (비차단)
    /// </summary>
    void Enqueue(ChatInteractionLog log);

    /// <summary>
    /// 저장된 모든 로그를 채널에서 적출하여 반환합니다.
    /// </summary>
    IAsyncEnumerable<ChatInteractionLog> DrainAllAsync(CancellationToken ct);

    /// <summary>
    /// 채널을 완료 상태로 변경합니다.
    /// </summary>
    void Complete();
}
