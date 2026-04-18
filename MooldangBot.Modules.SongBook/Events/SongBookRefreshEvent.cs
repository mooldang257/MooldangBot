using MediatR;
using MooldangBot.Domain.Common;
using System;

namespace MooldangBot.Modules.SongBook.Events;

/// <summary>
/// [오시리스의 파동]: 송북 및 오마카세 상태가 변경되어 오버레이의 새로고침이 필요함을 알립니다.
/// </summary>
public record SongBookRefreshEvent(string ChzzkUid, Guid CorrelationId = default) : INotification, IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
