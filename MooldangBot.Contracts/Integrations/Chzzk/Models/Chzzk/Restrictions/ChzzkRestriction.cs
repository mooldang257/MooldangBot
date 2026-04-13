using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Restrictions;

/// <summary>
/// [?ㅼ떆由ъ뒪??援ъ냽]: ?쒕룞 ?쒗븳(李⑤떒) 異붽? 諛???젣瑜??꾪븳 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkRestrictionRequest
{
    [JsonPropertyName("targetChannelId")]
    public string TargetChannelId { get; set; } = string.Empty;
}

/// <summary>
/// [?ㅼ떆由ъ뒪??二꾩닔]: ?쒕룞 ?쒗븳 ?곸꽭 ?뺣낫 ?곗씠?곗엯?덈떎.
/// </summary>
public class ChzzkRestrictionData
{
    [JsonPropertyName("restrictedChannelId")]
    public string? RestrictedChannelId { get; set; }

    [JsonPropertyName("restrictedChannelName")]
    public string? RestrictedChannelName { get; set; }

    [JsonPropertyName("createdDate")]
    public string? CreatedDate { get; set; }

    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }
}
