using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Session;

// [오시리스의 접속]: WebSocket 연결을 위한 URL 응답 모델입니다.
public class SessionUrlResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

// [오시리스의 청취]: 구독 중인 이벤트 정보를 담는 모델입니다.
public class SubscribedEventInfo
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;
}

// [오시리스의 목록]: 특정 채널의 활성 세션 정보 모델입니다.
public class SessionListItem
{
    [JsonPropertyName("sessionKey")]
    public string SessionKey { get; set; } = string.Empty;

    [JsonPropertyName("connectedDate")]
    public string ConnectedDate { get; set; } = string.Empty;

    [JsonPropertyName("disconnectedDate")]
    public string? DisconnectedDate { get; set; }

    [JsonPropertyName("subscribedEvents")]
    public List<SubscribedEventInfo> SubscribedEvents { get; set; } = new();
}

// [오시리스의 채택]: 세션에 이벤트를 구독하기 위한 요청 모델입니다.
public class SubscribeEventRequest
{
    [JsonPropertyName("sessionKey")]
    public string SessionKey { get; set; } = string.Empty;
}
