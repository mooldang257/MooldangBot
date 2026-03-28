using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [파로스의 통합]: 시스템의 모든 유료/무료 명령어를 통합 관리하는 엔티티입니다.
/// </summary>
[Index(nameof(ChzzkUid), nameof(Keyword), IsUnique = true)]
public class UnifiedCommand
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChzzkUid { get; set; } = string.Empty;

    [Required]
    public CommandCategory Category { get; set; } 

    [Required]
    [MaxLength(50)]
    public string Keyword { get; set; } = string.Empty;

    public int Cost { get; set; } = 0;

    [Required]
    public CommandCostType CostType { get; set; } 

    [MaxLength(1000)]
    public string ResponseText { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FeatureType { get; set; } = "Reply"; 

    public int? TargetId { get; set; }

    [Required]
    public CommandRole RequiredRole { get; set; } = CommandRole.Viewer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
