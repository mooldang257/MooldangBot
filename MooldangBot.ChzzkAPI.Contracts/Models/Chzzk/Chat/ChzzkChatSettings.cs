using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Chat;

/// <summary>
/// [?ㅼ떆由ъ뒪??洹쒖튃]: 梨꾨꼸??梨꾪똿 ?ㅼ젙 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkChatSettings
{
    [JsonPropertyName("chatAvailableCondition")]
    public string? ChatAvailableCondition { get; set; }

    [JsonPropertyName("chatAvailableGroup")]
    public string? ChatAvailableGroup { get; set; }

    [JsonPropertyName("minFollowerMinute")]
    public int MinFollowerMinute { get; set; }

    [JsonPropertyName("allowSubscriberInFollowerMode")]
    public bool AllowSubscriberInFollowerMode { get; set; }

    [JsonPropertyName("chatSlowModeSec")]
    public int ChatSlowModeSec { get; set; }

    [JsonPropertyName("chatEmojiMode")]
    public bool ChatEmojiMode { get; set; }
}
