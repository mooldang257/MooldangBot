using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [주인의 목소리]: SysStreamerKnowledges 테이블과 매핑되는 엔티티입니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(Keyword))]
public class SysStreamerKnowledges
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    [Required]
    [MaxLength(100)]
    public string Keyword { get; set; } = string.Empty;    // 감지할 핵심어 [대변인의 방패]

    [Required]
    public string IntentAnswer { get; set; } = string.Empty; // 스트리머가 의도한 정답

    public bool IsActive { get; set; } = true; // [v6.1.5] 기능 활성화 (삭제는 프로필 종속)

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
