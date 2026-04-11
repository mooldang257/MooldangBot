using System.Text.Json.Serialization;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Live;

/// <summary>
/// [?ㅼ떆由ъ뒪???몃?]: 諛⑹넚 ?곸꽭 ?뺣낫 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkLiveDetailContent
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("liveTitle")]
    public string? LiveTitle { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}
