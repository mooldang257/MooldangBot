using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Restrictions;

/// <summary>
/// [오시리스??移⑤У]: ?꾩떆 ?쒗븳(裕ㅽ듃) 異붽? 諛??댁젣瑜??꾪븳 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkTemporaryRestrictionRequest
{
    [JsonPropertyName("targetChannelId")]
    public string TargetChannelId { get; set; } = string.Empty;

    [JsonPropertyName("chatChannelId")]
    public string ChatChannelId { get; set; } = string.Empty;
}
