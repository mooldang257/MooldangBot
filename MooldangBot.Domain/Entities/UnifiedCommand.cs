using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [파로스의 통합 - v4.3 정문화]: 시스템의 모든 유료/무료 명령어를 통합 관리하는 엔티티입니다.
/// [v6.1] 정규화: ISoftDeletable, IAuditable 상속 및 명칭 통합.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(Keyword), nameof(FeatureType), IsUnique = true)]
public class UnifiedCommand : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    public CommandFeatureType FeatureType { get; set; } = CommandFeatureType.Reply;

    [Required]
    [MaxLength(50)]
    public string Keyword { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Icon { get; set; } = "🎵";

    public int Cost { get; set; } = 0;

    [Required]
    public CommandCostType CostType { get; set; } 

    [MaxLength(1000)]
    public string ResponseText { get; set; } = string.Empty;

    public int? TargetId { get; set; }

    /// <summary>
    /// [v6.2.2] 명령어 매칭 방식 (Exact, Prefix, Contains, Regex)
    /// </summary>
    public CommandMatchType MatchType { get; set; } = CommandMatchType.Exact;

    /// <summary>
    /// [v6.2.2] 키워드 뒤에 공백이 필요한지 여부 (Prefix 매칭용)
    /// </summary>
    public bool RequiresSpace { get; set; } = true;

    [Required]
    public CommandRole RequiredRole { get; set; } = CommandRole.Viewer;

    // [v6.1] 정규화: IsActive(bool) -> IsDeleted(bool, ISoftDeletable)
    public bool IsActive { get; set; } = true; // [v6.1.5] 기능 활성화 (스트리머 토글용)

    public bool IsDeleted { get; set; } = false; // [v6.1.5] 존재 거버넌스 (물리적 배사용)
    public KstClock? DeletedAt { get; set; }

    [Required]
    public KstClock CreatedAt { get; set; } = KstClock.Now;

    public KstClock? UpdatedAt { get; set; }
}
