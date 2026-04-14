using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v7.0] 시청자 관계 엔티티: 스트리머와 시청자 간의 관계 및 활동 정보(출석 등)를 관리합니다.
/// </summary>
[Table("viewer_relations")]
[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
public class ViewerRelation : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual GlobalViewer? GlobalViewer { get; set; }

    /// <summary>
    /// 총 출석 횟수
    /// </summary>
    public int AttendanceCount { get; set; } = 0;

    /// <summary>
    /// 연속 출석 횟수
    /// </summary>
    public int ConsecutiveAttendanceCount { get; set; } = 0;

    /// <summary>
    /// 마지막 출석 일시
    /// </summary>
    public KstClock? LastAttendanceAt { get; set; }

    /// <summary>
    /// 최초 방문 일시
    /// </summary>
    public KstClock FirstVisitAt { get; set; } = KstClock.Now;

    /// <summary>
    /// 마지막 채팅 일시
    /// </summary>
    public KstClock? LastChatAt { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
