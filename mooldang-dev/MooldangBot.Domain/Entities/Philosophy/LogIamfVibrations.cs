using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [피닉스의 눈금]: LogIamfVibrations 테이블과 매핑되는 시계열 엔티티입니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(CreatedAt))]
public class LogIamfVibrations
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    public double RawHz { get; set; }                    // 실제 계산된 진동수

    public double EmaHz { get; set; }                    // 지수 이동 평균 (추세)

    public double StabilityScore { get; set; }           // 공명 안정도 (0.0~1.0)

    public KstClock CreatedAt { get; set; } = KstClock.Now;
}
