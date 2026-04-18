using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Chat;

// [오시리스??諛쒗솕]: 梨꾪똿 전송 ?붿껌 紐⑤뜽?낅땲??
public class SendChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// [오시리스???꾩뼵]: 梨꾪똿 전송 ?깃났 ?묐떟 紐⑤뜽?낅땲??
public class SendChatResponse
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;
}

// [오시리스??怨듯몴]: 梨꾪똿 공지 설정 ?붿껌 紐⑤뜽?낅땲??
public class SetChatNoticeRequest
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}

// [오시리스??洹쒖쑉]: 梨꾨꼸 梨꾪똿 설정(?붾줈???꾩슜 ?? 紐⑤뜽?낅땲??
public class ChatSettings
{
    [JsonPropertyName("chatAvailableCondition")]
    public string ChatAvailableCondition { get; set; } = string.Empty;

    [JsonPropertyName("chatAvailableGroup")]
    public string ChatAvailableGroup { get; set; } = string.Empty;

    [JsonPropertyName("minFollowerMinute")]
    public int MinFollowerMinute { get; set; }

    [JsonPropertyName("allowSubscriberInFollowerMode")]
    public bool AllowSubscriberInFollowerMode { get; set; }

    [JsonPropertyName("chatSlowModeSec")]
    public int ChatSlowModeSec { get; set; }

    [JsonPropertyName("chatEmojiMode")]
    public bool ChatEmojiMode { get; set; }
}

// [오시리스???뺥솕]: 특정 硫붿떆吏瑜?블라인드 泥섎━?섍린 ?꾪븳 ?붿껌 紐⑤뜽?낅땲??
public class BlindMessageRequest
{
    [JsonPropertyName("chatChannelId")]
    public string ChatChannelId { get; set; } = string.Empty;

    [JsonPropertyName("messageTime")]
    public long MessageTime { get; set; }

    [JsonPropertyName("senderChannelId")]
    public string SenderChannelId { get; set; } = string.Empty;
}
