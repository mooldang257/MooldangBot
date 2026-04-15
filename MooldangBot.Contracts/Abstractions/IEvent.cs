using MediatR;

namespace MooldangBot.Contracts.Abstractions;

/// <summary>
/// [오시리스의 인장]: 분산 시스템 전역에서 통용되는 모든 이벤트의 기반 인터페이스입니다.
/// 모든 이벤트는 고유 식별자와 발생 시간을 강제해야 합니다.
/// </summary>
public interface IEvent : INotification
{
    public Guid EventId { get; }
    public Guid CorrelationId { get; }
    public DateTime OccurredOn { get; }
}

