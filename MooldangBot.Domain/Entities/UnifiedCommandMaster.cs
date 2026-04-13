using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [마스터 데이터]: 통합 명령어 카테고리 정의 (General, Fixed, Donation 등)
/// </summary>
public class Master_CommandCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // 마스터 데이터이므로 직접 ID 지정
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

/// <summary>
/// [마스터 데이터]: 명령어별 고유 기능(Feature) 상세 정의
/// </summary>
public class Master_CommandFeature
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // 마스터 데이터이므로 직접 ID 지정
    public int Id { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Master_CommandCategory? Category { get; set; }

    [Required]
    [MaxLength(50)]
    public string TypeName { get; set; } = string.Empty; // Reply, Song, Roulette 등

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public int DefaultCost { get; set; }

    public CommandRole RequiredRole { get; set; } = CommandRole.Viewer;

    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// [마스터 데이터]: [v1.8] 챗봇 답변용 동적 변수 및 쿼리 정의
/// </summary>
public class Master_DynamicVariable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Keyword { get; set; } = string.Empty; // 예: {포인트}

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string BadgeColor { get; set; } = "primary"; // primary, success, info 등

    [Required]
    [MaxLength(500)]
    public string QueryString { get; set; } = string.Empty; // SELECT ... FROM ...
}
