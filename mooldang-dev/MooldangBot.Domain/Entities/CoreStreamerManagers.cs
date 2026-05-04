using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

// [v4.7 정규화] 한 스트리머당 동일한 시청자가 중복으로 매니저 등록되는 것을 방지하기 위한 복합 유니크 인덱스
[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
public class CoreStreamerManagers
{
    [Key]
    public int Id { get; set; }

    // 1. [정규화] StreamerChzzkUid -> StreamerProfileId
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    // 2. [정규화] ManagerChzzkUid -> GlobalViewerId
    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual CoreGlobalViewers? CoreGlobalViewers { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = "manager"; // "manager", "admin" 등 확장 가능

    public KstClock CreatedAt { get; set; } = KstClock.Now; // KST
}
