using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Chat;

// [?ㅼ떆由ъ뒪??諛쒗솕]: 梨꾪똿 ?꾩넚 ?붿껌 紐⑤뜽?낅땲??
public class SendChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪???꾩뼵]: 梨꾪똿 ?꾩넚 ?깃났 ?묐떟 紐⑤뜽?낅땲??
public class SendChatResponse
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪??怨듯몴]: 梨꾪똿 怨듭? ?ㅼ젙 ?붿껌 紐⑤뜽?낅땲??
public class SetChatNoticeRequest
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}

// [?ㅼ떆由ъ뒪??洹쒖쑉]: 梨꾨꼸 梨꾪똿 ?ㅼ젙(?붾줈???꾩슜 ?? 紐⑤뜽?낅땲??
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

// [?ㅼ떆由ъ뒪???뺥솕]: ?뱀젙 硫붿떆吏瑜?釉붾씪?몃뱶 泥섎━?섍린 ?꾪븳 ?붿껌 紐⑤뜽?낅땲??
public class BlindMessageRequest
{
    [JsonPropertyName("chatChannelId")]
    public string ChatChannelId { get; set; } = string.Empty;

    [JsonPropertyName("messageTime")]
    public long MessageTime { get; set; }

    [JsonPropertyName("senderChannelId")]
    public string SenderChannelId { get; set; } = string.Empty;
}
