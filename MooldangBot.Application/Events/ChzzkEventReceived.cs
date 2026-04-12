using MediatR;
using MooldangBot.ChzzkAPI.Contracts.Models.Events;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Events;

/// <summary>
/// [v3.7 ?Œë¡œ?¤ì˜ ?„ë???: ì¹˜ì?ì§?ê²Œì´?¸ì›¨?´ì—???˜ì‹ ???¤í˜•???°ì´?°ë? ?¨ë? ?´ë?ë¡??¤ì–´ ?˜ë¥´??ìµœì‹ ???´ë²¤??ë´‰íˆ¬?…ë‹ˆ??
/// </summary>
public record ChzzkEventReceived(
    Guid MessageId,
    StreamerProfile Profile,
    ChzzkEventBase Payload,
    DateTimeOffset ReceivedAt
) : INotification;

