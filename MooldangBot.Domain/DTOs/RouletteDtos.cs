using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [v10.0] 룰렛 상세 조회를 위한 하이드레이션 완료된 DTO (순환 참조 방지)
/// </summary>
public class RouletteResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public RouletteType Type { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("costPerSpin")]
    public int CostPerSpin { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("items")]
    public List<RouletteItemResponseDto> Items { get; set; } = new();

    [JsonPropertyName("updatedAt")]
    public KstClock? UpdatedAt { get; set; }
}

/// <summary>
/// [v10.0] 룰렛 아이템 상세 정보 (역참조 제거)
/// </summary>
public class RouletteItemResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("probability10x")]
    public double Probability10x { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#3498db";

    [JsonPropertyName("isMission")]
    public bool IsMission { get; set; }

    [JsonPropertyName("template")]
    public string Template { get; set; } = "Standard";

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("soundUrl")]
    public string? SoundUrl { get; set; }

    [JsonPropertyName("useDefaultSound")]
    public bool UseDefaultSound { get; set; } = true;
}
