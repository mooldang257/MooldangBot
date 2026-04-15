using MediatR;
using MooldangBot.Contracts.Abstractions;
using System;

namespace MooldangBot.Contracts.SongBook.Events;

/// <summary>
/// [오시리스의 곡명]: 새로운 노래 신청이 성공적으로 접수되었음을 알리는 통합 이벤트입니다.
/// </summary>
public record SongAddedEvent(string Username, string SongTitle, string? ChzzkUid = null, Guid CorrelationId = default) : INotification, IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
