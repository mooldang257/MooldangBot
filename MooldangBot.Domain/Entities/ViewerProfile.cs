using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v4.2] 채널별 시청자 프로필: 개별 스트리머 채널에서의 시청자 포인트 및 스탯을 관리합니다.
/// 3제정규형(3NF)에 따라 스트리머와 글로벌 시청자를 ID(int)로 연결하여 DB 용량을 최적화합니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
[Index(nameof(StreamerProfileId), nameof(Points))]
public class ViewerProfile
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// [정규화] 부모 스트리머 프로필 ID
    /// </summary>
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    /// <summary>
    /// [정규화] 글로벌 시청자 마스터 ID
    /// </summary>
    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual GlobalViewer? GlobalViewer { get; set; }

    /// <summary>
    /// 해당 채널에서의 닉네임 (스트리머별로 다를 수 있음)
    /// </summary>
    [MaxLength(100)]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 현재 보유 포인트 (동시성 제어 적용)
    /// </summary>
    [ConcurrencyCheck]
    public int Points { get; set; } = 0;

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
}
