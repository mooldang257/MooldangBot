using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [v10.0] 룰렛 상세 조회를 위한 하이드레이션 완료된 DTO (순환 참조 방지)
/// </summary>
public class RouletteResponseDto
{
    public int Id { get; set; }
 
    public string Name { get; set; } = string.Empty;
 
    public RouletteType Type { get; set; }
 
    public string Command { get; set; } = string.Empty;
 
    public int CostPerSpin { get; set; }
 
    public bool IsActive { get; set; }
 
    public List<RouletteItemResponseDto> Items { get; set; } = new();
 
    public KstClock? UpdatedAt { get; set; }
}
 
/// <summary>
/// [v10.0] 룰렛 아이템 상세 정보 (역참조 제거)
/// </summary>
public class RouletteItemResponseDto
{
    public int Id { get; set; }
 
    public string ItemName { get; set; } = string.Empty;
 
    public double Probability { get; set; }
 
    public double Probability10x { get; set; }
 
    public string Color { get; set; } = "#3498db";
 
    public bool IsMission { get; set; }
 
    public string Template { get; set; } = "Standard";
 
    public bool IsActive { get; set; } = true;
 
    public string? SoundUrl { get; set; }
 
    public bool UseDefaultSound { get; set; } = true;
}
