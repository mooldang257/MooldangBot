using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [파로스의 통합 - v4.3 정문화]: 시스템의 모든 유료/무료 명령어를 통합 관리하는 엔티티입니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(Keyword), IsUnique = true)]
public class UnifiedCommand
{
    [Key]
    public int Id { get; set; }

    // ----------------------------------------------------
    // [정문화 영역 1] Streamer 연결
    // ----------------------------------------------------
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    // ----------------------------------------------------
    // [정문화 영역 2] 마스터 기능 연결 (Category와 FeatureType 대체)
    // ----------------------------------------------------
    [Required]
    public int MasterCommandFeatureId { get; set; }

    [ForeignKey(nameof(MasterCommandFeatureId))]
    public virtual Master_CommandFeature? MasterFeature { get; set; }

    // ----------------------------------------------------
    // 명령어 고유 속성 (스트리머별 커스텀 설정)
    // ----------------------------------------------------
    [Required]
    [MaxLength(50)]
    public string Keyword { get; set; } = string.Empty;

    public int Cost { get; set; } = 0;

    [Required]
    public CommandCostType CostType { get; set; } 

    [MaxLength(1000)]
    public string ResponseText { get; set; } = string.Empty;

    public int? TargetId { get; set; }

    [Required]
    public CommandRole RequiredRole { get; set; } = CommandRole.Viewer;

    public bool IsActive { get; set; } = true;

    [Required]
    public KstClock CreatedAt { get; set; } = KstClock.Now;

    public KstClock? UpdatedAt { get; set; }
}
