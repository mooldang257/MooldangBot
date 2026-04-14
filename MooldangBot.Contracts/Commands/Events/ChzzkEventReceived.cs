using MediatR;
using MooldangBot.Contracts.Abstractions;
using MooldangBot.Contracts.Chzzk.Models.Events;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Commands.Events;

/// <summary>
/// [v3.7 오시리스의 수신함]: 치지직 게이트웨이로부터 수신된 정형화된 데이터인 최신 이벤트 봉투입니다.
/// </summary>
public record ChzzkEventReceived(
    Guid MessageId,
    StreamerProfile Profile,
    ChzzkEventBase Payload,
    DateTimeOffset ReceivedAt
) : INotification, IEvent
{
    // [v4.1] Audit: IEvent 인터페이스 강제 구현 (MessageId 및 ReceivedAt 매핑)
    public Guid EventId => MessageId;
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => ReceivedAt.UtcDateTime;
}

