using System;
using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Channels;

// [?ㅼ떆由ъ뒪???곸?]: 梨꾨꼸 ?꾨줈??諛??뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class ChannelProfile
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("channelImageUrl")]
    public string? ChannelImageUrl { get; set; }

    [JsonPropertyName("followerCount")]
    public int FollowerCount { get; set; }

    [JsonPropertyName("verifiedMark")]
    public bool VerifiedMark { get; set; }
}

// [?ㅼ떆由ъ뒪??蹂댁쥖]: 梨꾨꼸 愿由ъ옄 ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class ChannelManager
{
    [JsonPropertyName("managerChannelId")]
    public string ManagerChannelId { get; set; } = string.Empty;

    [JsonPropertyName("managerChannelName")]
    public string ManagerChannelName { get; set; } = string.Empty;

    [JsonPropertyName("userRole")]
    public string UserRole { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}

// [?ㅼ떆由ъ뒪??異붿쥌]: 梨꾨꼸 ?붾줈???뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class ChannelFollower
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}

// [?ㅼ떆由ъ뒪??議곌났]: 梨꾨꼸 援щ룆??諛??곗뼱 ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class ChannelSubscriber
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("tierNo")]
    public int TierNo { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}
