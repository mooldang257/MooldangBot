using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Session;

// [?ㅼ떆由ъ뒪???묒냽]: WebSocket ?곌껐???꾪븳 URL ?묐떟 紐⑤뜽?낅땲??
public class SessionUrlResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪??泥?랬]: 援щ룆 以묒씤 ?대깽???뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class SubscribedEventInfo
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪??紐⑸줉]: ?뱀젙 梨꾨꼸???쒖꽦 ?몄뀡 ?뺣낫 紐⑤뜽?낅땲??
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

// [?ㅼ떆由ъ뒪??梨꾪깮]: ?몄뀡???대깽?몃? 援щ룆?섍린 ?꾪븳 ?붿껌 紐⑤뜽?낅땲??
public class SubscribeEventRequest
{
    [JsonPropertyName("sessionKey")]
    public string SessionKey { get; set; } = string.Empty;
}
