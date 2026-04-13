using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Categories;

/// <summary>
/// [?ㅼ떆由ъ뒪??遺꾨쪟]: 移댄뀒怨좊━(寃뚯엫, 二쇱젣 ?? ?뺣낫 ?곗씠?곗엯?덈떎.
/// </summary>
public class ChzzkCategoryData
{
    [JsonPropertyName("categoryType")]
    public string? CategoryType { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("categoryValue")]
    public string? CategoryValue { get; set; }

    [JsonPropertyName("posterImageUrl")]
    public string? PosterImageUrl { get; set; }
}
