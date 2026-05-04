using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [피닉스의 기록]: IamfScenarios 테이블과 매핑되는 엔티티입니다.
/// </summary>
public class IamfScenarios : ISoftDeletable, IAuditable
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 추가

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    [Required]
    [MaxLength(100)]
    public string ScenarioId { get; set; } = string.Empty;

    public int Level { get; set; } = 1;

    [Required]
    public string Content { get; set; } = string.Empty;

    public double VibrationHz { get; set; }

    public bool IsActive { get; set; } = true; // [v6.1.5] 시나리오 활성화 (토글용)

    // [v6.1] 정규화: ISoftDeletable, IAuditable 구현
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
