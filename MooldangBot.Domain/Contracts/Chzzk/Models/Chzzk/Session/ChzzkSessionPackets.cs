using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Session;

/// <summary>
/// [오시리스의 지령]: SYSTEM 이벤트 본문에서 sessionKey를 추출하기 위한 모델입니다.
/// </summary>
public class ChzzkSystemEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public ChzzkSystemEventData? Data { get; set; }
}

public class ChzzkSystemEventData
{
    [JsonPropertyName("sessionKey")]
    public string SessionKey { get; set; } = string.Empty;
}

/// <summary>
/// [v4.0] CHAT 전용 페이로드 모델 (순수 채팅 목적)
/// </summary>
public class ChzzkChatPayload
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("messageTime")]
    public long MessageTime { get; set; }

    [JsonPropertyName("profile")]
    public System.Text.Json.JsonElement? Profile { get; set; }

    [JsonPropertyName("emojis")]
    public System.Text.Json.JsonElement? Emojis { get; set; }

    [JsonPropertyName("senderChannelId")]
    public string? SenderChannelId { get; set; }

    [JsonPropertyName("chatChannelId")]
    public string? ChatChannelId { get; set; }

    [JsonPropertyName("userRoleCode")]
    public string? UserRoleCode { get; set; }

    [JsonPropertyName("extras")]
    public System.Text.Json.JsonElement? Extra { get; set; }
}

/// <summary>
/// [v4.0] DONATION 전용 페이로드 모델 (결제 및 후원 목적)
/// </summary>
public class ChzzkDonationPayload
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("donationType")]
    public string? DonationType { get; set; }

    [JsonPropertyName("donatorChannelId")]
    public string? DonatorChannelId { get; set; }

    [JsonPropertyName("donatorNickname")]
    public string? DonatorNickname { get; set; }

    [JsonPropertyName("payAmount")]
    public System.Text.Json.JsonElement? PayAmount { get; set; }

    [JsonPropertyName("donationText")]
    public string? DonationText { get; set; }

    [JsonPropertyName("userRoleCode")]
    public string? UserRoleCode { get; set; }

    [JsonPropertyName("messageTime")]
    public long MessageTime { get; set; }

    [JsonPropertyName("profile")]
    public System.Text.Json.JsonElement? Profile { get; set; }

    [JsonPropertyName("extras")]
    public System.Text.Json.JsonElement? Extra { get; set; }
}

/// <summary>
/// [v4.0] SUBSCRIPTION 전용 페이로드 모델 (구독 목적)
/// </summary>
public class ChzzkSubscriptionPayload
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("subscriberChannelId")]
    public string? SubscriberChannelId { get; set; }

    [JsonPropertyName("subscriberNickname")]
    public string? SubscriberNickname { get; set; }

    [JsonPropertyName("tierNo")]
    public int? TierNo { get; set; }

    [JsonPropertyName("month")]
    public int? Month { get; set; }

    [JsonPropertyName("profile")]
    public System.Text.Json.JsonElement? Profile { get; set; }

    [JsonPropertyName("extras")]
    public System.Text.Json.JsonElement? Extra { get; set; }
}

/// <summary>
/// [오시리스의 인장]: CHAT 이벤트의 프로필 정보 모델입니다.
/// </summary>
public class ChzzkChatProfile
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("verifiedMark")]
    public bool VerifiedMark { get; set; }
}
