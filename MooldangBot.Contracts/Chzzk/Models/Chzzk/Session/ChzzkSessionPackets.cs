using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Session;

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
/// [오시리스의 파동]: CHAT/DONATION/SUBSCRIPTION 통합 이벤트 페이로드 모델.
/// 공식 OpenAPI 명세(Session.md)의 모든 루트 필드를 포함합니다.
/// </summary>
public class ChzzkChatPayload
{
    // 공통 필드
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

    // CHAT 전용 루트 필드
    [JsonPropertyName("senderChannelId")]
    public string? SenderChannelId { get; set; }

    [JsonPropertyName("chatChannelId")]
    public string? ChatChannelId { get; set; }

    [JsonPropertyName("userRoleCode")]
    public string? UserRoleCode { get; set; }

    // DONATION 전용 루트 필드
    [JsonPropertyName("donationType")]
    public string? DonationType { get; set; }

    [JsonPropertyName("donatorChannelId")]
    public string? DonatorChannelId { get; set; }

    [JsonPropertyName("donatorNickname")]
    public string? DonatorNickname { get; set; }

    [JsonPropertyName("payAmount")]
    public System.Text.Json.JsonElement? PayAmount { get; set; } // String 또는 Number 대응

    [JsonPropertyName("donationText")]
    public string? DonationText { get; set; }

    // SUBSCRIPTION 전용 루트 필드
    [JsonPropertyName("subscriberChannelId")]
    public string? SubscriberChannelId { get; set; }

    [JsonPropertyName("subscriberNickname")]
    public string? SubscriberNickname { get; set; }

    [JsonPropertyName("tierNo")]
    public int? TierNo { get; set; }

    [JsonPropertyName("tierName")]
    public string? TierName { get; set; }

    [JsonPropertyName("month")]
    public int? Month { get; set; }

    // 하위 호환성 및 확장용
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
