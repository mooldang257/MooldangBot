using MooldangBot.Domain.Common;

namespace MooldangBot.Contracts.Events;

/// <summary>
/// [오시리스의 전령]: 치지직 등 외부 플랫폼에서 수집되어 함대 내부에 전파되는 통합 채팅 이벤트 모델입니다.
/// </summary>
public sealed record ChatReceivedEvent : IEvent
{
    // IEvent 강제 구현
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public required string ChannelId { get; init; }
    public required string PlatformUserId { get; init; }
    public required string Nickname { get; init; }
    public required string Content { get; init; }
    
    public string? UserRole { get; init; }
    public bool IsSubscriber { get; init; }
    public int SubscriptionTier { get; init; }
    public int PayAmount { get; init; }
    public string? EmojisJson { get; init; }
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
