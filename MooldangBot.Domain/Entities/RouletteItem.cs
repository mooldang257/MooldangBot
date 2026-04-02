using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [행운의 눈금]: 룰렛의 각 당첨 항목 정보를 담는 엔티티입니다.
/// </summary>
public class RouletteItem : IAuditable
{
    [Key]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("rouletteId")]
    public int RouletteId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(RouletteId))]
    public virtual Roulette? Roulette { get; set; }

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("probability10x")]
    public double Probability10x { get; set; }

    [MaxLength(20)]
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";

    [JsonPropertyName("isMission")]
    public bool IsMission { get; set; } = false;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public KstClock CreatedAt { get; set; } = KstClock.Now;

    [JsonPropertyName("updatedAt")]
    public KstClock? UpdatedAt { get; set; }
}
