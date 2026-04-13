using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v6.2] 스트리머 채널별 시청자 프로필: 개별 스트리머 채널에서의 시청자 포인트 및 스탯을 관리합니다.
/// 공통 정보(Nickname 등)는 GlobalViewer로 이관되어 중복이 제거되었습니다.
/// </summary>
[Table("view_streamer_viewers")]
[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
[Index(nameof(StreamerProfileId), nameof(Points))]
public class View_StreamerViewer : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 부모 스트리머 프로필 ID
    /// </summary>
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    /// <summary>
    /// 글로벌 시청자 마스터 ID (엔티티 중심축)
    /// </summary>
    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual GlobalViewer? GlobalViewer { get; set; }

    /// <summary>
    /// 현재 보유 포인트 (동시성 제어 적용)
    /// </summary>
    [ConcurrencyCheck]
    public int Points { get; set; } = 0;

    /// <summary>
    /// 보유 후원 잔액 (DonationPoints)
    /// </summary>
    [ConcurrencyCheck]
    public int DonationPoints { get; set; } = 0;

    /// <summary>
    /// 총 출석 횟수 (동시성 제어 적용)
    /// </summary>
    [ConcurrencyCheck]
    public int AttendanceCount { get; set; } = 0;

    /// <summary>
    /// 연속 출석 횟수
    /// </summary>
    public int ConsecutiveAttendanceCount { get; set; } = 0;

    /// <summary>
    /// 마지막 출석 일시
    /// </summary>
    public KstClock? LastAttendanceAt { get; set; }

    public bool IsActive { get; set; } = true; 

    public bool IsDeleted { get; set; } = false; 
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;

    [ConcurrencyCheck]
    public KstClock? UpdatedAt { get; set; }
}
