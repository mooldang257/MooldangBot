using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Live;

/// <summary>
/// [오시리스의 감시]: 라이브 방송의 상세 상태 정보를 담는 모델입니다.
/// </summary>
public class LiveDetailResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("liveTitle")]
    public string? LiveTitle { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}
