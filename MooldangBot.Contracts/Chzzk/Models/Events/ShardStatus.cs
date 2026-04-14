using System;
using MooldangBot.Contracts.Abstractions;

namespace MooldangBot.Contracts.Chzzk.Models.Events;

/// <summary>
/// [오시리스의 심박]: 개별 샤드의 연결 상태 및 건강 상태를 나타내는 레코드입니다.
/// </summary>
public record ShardStatus(int ShardId, int ConnectionCount, bool IsHealthy) : IEvent
{
    // [v4.1] Audit: IEvent 인터페이스 강제 구현
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
