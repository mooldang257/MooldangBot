using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MooldangBot.Contracts.Abstractions;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Events;

/// <summary>
/// [오시리스의 영혼]: 치지직 이벤트의 다형성 베이스 클래스입니다. (v3.7)
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType")]
[JsonDerivedType(typeof(ChzzkChatEvent), "chat")]
[JsonDerivedType(typeof(ChzzkDonationEvent), "donation")]
[JsonDerivedType(typeof(ChzzkSubscriptionEvent), "subscription")]
public abstract record ChzzkEventBase : IEvent
{
    // IEvent 구현
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => Timestamp;

    public required string ChannelId { get; init; }
    public required string SenderId { get; init; }
    public required string Nickname { get; init; }
    public string? UserRoleCode { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// 일반 채팅 이벤트
/// </summary>
public record ChzzkChatEvent : ChzzkEventBase
{
    public required string Content { get; init; }
    public string? ChatChannelId { get; init; }
    public JsonElement? Emojis { get; init; }
}

/// <summary>
/// 후원 이벤트 (텍스트/영상 통합)
/// </summary>
public record ChzzkDonationEvent : ChzzkEventBase
{
    public int PayAmount { get; init; }
    public required string DonationMessage { get; init; }
    public bool IsVideoDonation { get; init; }
}

/// <summary>
/// 구독 이벤트
/// </summary>
public record ChzzkSubscriptionEvent : ChzzkEventBase
{
    public int SubscriptionTier { get; init; }
    public int SubscriptionMonth { get; init; }
}
