using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Categories;

// [오시리스의 검색]: 카테고리 검색 결과 항목을 담는 모델입니다.
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
