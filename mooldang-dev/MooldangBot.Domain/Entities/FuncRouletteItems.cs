using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [행운의 눈금]: 룰렛의 각 당첨 항목 정보를 담는 엔티티입니다.
/// </summary>
public class FuncRouletteItems : IAuditable
{
    [Key]
    public int Id { get; set; }

    public int RouletteId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(RouletteId))]
    public virtual FuncRouletteMain? FuncRouletteMain { get; set; }

    [Required]
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;

    public double Probability { get; set; }

    public double Probability10x { get; set; }

    [MaxLength(20)]
    public string Color { get; set; } = "#FFFFFF";

    public bool IsMission { get; set; } = false;

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string Template { get; set; } = "Standard";

    [MaxLength(500)]
    public string? SoundUrl { get; set; }

    public bool UseDefaultSound { get; set; } = true;

    public KstClock CreatedAt { get; set; } = KstClock.Now;

    public KstClock? UpdatedAt { get; set; }
}
