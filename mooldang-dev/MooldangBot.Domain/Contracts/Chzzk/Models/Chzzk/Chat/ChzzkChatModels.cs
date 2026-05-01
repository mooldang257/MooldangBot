using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Chat;

// [오시리스의 발화]: 채팅 전송 요청 모델입니다.
public class SendChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// [오시리스의 전언]: 채팅 전송 성공 응답 모델입니다.
public class SendChatResponse
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;
}

// [오시리스의 공표]: 채팅 공지 설정 요청 모델입니다.
public class SetChatNoticeRequest
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}

// [오시리스의 규율]: 채널 채팅 설정(팔로워 전용 등) 모델입니다.
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

// [오시리스의 정화]: 특정 메시지를 블라인드 처리하기 위한 요청 모델입니다.
public class BlindMessageRequest
{
    [JsonPropertyName("chatChannelId")]
    public string ChatChannelId { get; set; } = string.Empty;

    [JsonPropertyName("messageTime")]
    public long MessageTime { get; set; }

    [JsonPropertyName("senderChannelId")]
    public string SenderChannelId { get; set; } = string.Empty;
}
