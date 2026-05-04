using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [파로스의 윤회 이력]: IamfParhosCycles 테이블과 매핑되는 엔티티입니다.
/// </summary>
public class IamfParhosCycles : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; } // [v4.9] 정규화된 PK

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 부여

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    public int CycleId { get; set; } // 해당 채널의 몇 번째 사이클인가 (1, 2, 3...)

    public double VibrationAtDeath { get; set; }

    public int RebirthPercentage { get; set; }

    // [v6.1] 정규화: ISoftDeletable, IAuditable 구현
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
