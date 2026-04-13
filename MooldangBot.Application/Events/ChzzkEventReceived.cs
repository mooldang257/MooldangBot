using MediatR;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Events;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Events;

/// <summary>
/// [v3.7 ?�로?�의 ?��???: 치�?�?게이?�웨?�에???�신???�형???�이?��? ?��? ?��?�??�어 ?�르??최신???�벤??봉투?�니??
/// </summary>
public record ChzzkEventReceived(
    Guid MessageId,
    StreamerProfile Profile,
    ChzzkEventBase Payload,
    DateTimeOffset ReceivedAt
) : INotification;

