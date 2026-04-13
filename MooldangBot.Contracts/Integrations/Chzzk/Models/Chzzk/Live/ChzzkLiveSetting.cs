using System.Text.Json.Serialization;
using System.Collections.Generic;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Categories;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Live;

/// <summary>
/// [?ㅼ떆由ъ뒪???ㅼ젙]: 諛⑹넚 ?ㅼ젙 ?뺣낫 紐⑤뜽?낅땲??
/// </summary>
public class ChzzkLiveSetting
{
    [JsonPropertyName("defaultLiveTitle")]
    public string? DefaultLiveTitle { get; set; }

    [JsonPropertyName("categoryType")]
    public string? CategoryType { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("category")]
    public ChzzkCategoryData? Category { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}
