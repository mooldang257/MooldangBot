using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Categories;

// [오시리스???섏깋]: 移댄뀒怨좊━ 寃??寃곌낵 ??ぉ???대뒗 紐⑤뜽?낅땲??
public class CategorySearchItem
{
    [JsonPropertyName("categoryType")]
    public string CategoryType { get; set; } = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("categoryValue")]
    public string CategoryValue { get; set; } = string.Empty;

    [JsonPropertyName("posterImageUrl")]
    public string? PosterImageUrl { get; set; }
}
