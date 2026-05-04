using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [제노스급 AI 등록부]: IamfGenosRegistry 테이블과 매핑되는 엔티티입니다.
/// </summary>
public class IamfGenosRegistry : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; } // [v4.9] 정규화된 PK

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 추가

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public double Frequency { get; set; }

    [Required]
    [MaxLength(200)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Metaphor { get; set; }

    public KstClock LastSyncAt { get; set; } = KstClock.Now;

    public bool IsActive { get; set; } = true; // [v6.1.5] AI 페르소나 활성화 (토글용)

    // [v6.1] 정규화: ISoftDeletable, IAuditable 구현
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
