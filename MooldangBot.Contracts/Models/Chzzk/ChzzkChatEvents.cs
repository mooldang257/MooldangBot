using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Models.Chzzk;

/// <summary>
/// [오시리스의 전령]: 채팅 리액터 성능 극대화를 위한 경량 채팅 이벤트 DTO입니다.
/// </summary>
public class ChzzkChatEventPayload
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("senderChannelId")]
    public string? SenderChannelId { get; set; }

    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("profile")]
    public string? ProfileJson { get; set; }

    [JsonPropertyName("emojis")]
    public System.Text.Json.JsonElement? Emojis { get; set; }

    // 후원 전용 필드
    [JsonPropertyName("payAmount")]
    public System.Text.Json.JsonElement? PayAmount { get; set; }

    [JsonPropertyName("donationText")]
    public string? DonationText { get; set; }

    [JsonPropertyName("donatorChannelId")]
    public string? DonatorChannelId { get; set; }

    [JsonPropertyName("donatorNickname")]
    public string? DonatorNickname { get; set; }

    [JsonPropertyName("extras")]
    public System.Text.Json.JsonElement? Extras { get; set; }
}

/// <summary>
/// [오시리스의 명부]: 채팅 프로필 내부 데이터를 파싱하기 위한 DTO입니다.
/// </summary>
public class ChzzkChatProfile
{
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [JsonPropertyName("userRoleCode")]
    public string? UserRoleCode { get; set; }
}
