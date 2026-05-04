using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// IamfStreamerSettings 테이블과 매핑되는 엔티티입니다.
/// </summary>
public class IamfStreamerSettings
{
    // [정규화] PK이자 FK로 활용하여 완벽한 1:1 관계 구성
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    public bool IsIamfEnabled { get; set; } = true;

    /// <summary>
    /// [스트리머의 통제권 확장]: 시각적 오버레이 공명 활성화 여부
    /// </summary>
    public bool IsVisualResonanceEnabled { get; set; } = true;

    /// <summary>
    /// [스트리머의 통제권 확장]: AI 페르소나 채팅 활성화 여부
    /// </summary>
    public bool IsPersonaChatEnabled { get; set; } = true;

    public double SensitivityMultiplier { get; set; } = 1.0;

    public double OverlayOpacity { get; set; } = 0.5;

    public KstClock LastUpdatedAt { get; set; } = KstClock.Now;
}
