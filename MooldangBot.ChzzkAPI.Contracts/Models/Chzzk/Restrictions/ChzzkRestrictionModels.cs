using System;
using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Restrictions;

// [?ㅼ떆由ъ뒪???쒖젣]: 梨꾨꼸 ?쒕룞 ?쒗븳(諛?裕ㅽ듃) ?붿껌 紐⑤뜽?낅땲??
public class ChannelRestrictionRequest
{
    [JsonPropertyName("targetChannelId")]
    public string TargetChannelId { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪???좎삁]: ?꾩떆 ?쒕룞 ?쒗븳 ?붿껌 紐⑤뜽?낅땲??
public class TemporaryRestrictionRequest
{
    [JsonPropertyName("targetChannelId")]
    public string TargetChannelId { get; set; } = string.Empty;

    [JsonPropertyName("chatChannelId")]
    public string ChatChannelId { get; set; } = string.Empty;
}

// [?ㅼ떆由ъ뒪??紐낅?]: ?쒗븳??梨꾨꼸 ?뺣낫瑜??대뒗 紐⑤뜽?낅땲??
public class RestrictedChannel
{
    [JsonPropertyName("restrictedChannelId")]
    public string RestrictedChannelId { get; set; } = string.Empty;

    [JsonPropertyName("restrictedChannelName")]
    public string RestrictedChannelName { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("releaseDate")]
    public DateTime? ReleaseDate { get; set; }
}
