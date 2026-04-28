using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// 곡 신청을 위해 누적된 포인트(치즈) 정보를 관리하는 엔터티입니다.
/// 목표 금액 미달 시 이 테이블에 기록되며, 목표 달성 시 삭제됩니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(SongBookId))]
[Table("func_song_accumulations")]
public class SongAccumulation : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    /// <summary>
    /// 노래책의 곡 ID. (노래책에 없는 곡은 누적하지 않는 것이 원칙이나 확장성을 위해 nullable 유지)
    /// </summary>
    public int? SongBookId { get; set; }

    [ForeignKey(nameof(SongBookId))]
    public virtual SongBook? SongBook { get; set; }

    /// <summary>
    /// 노래책에 없는 곡일 경우 검색을 위한 제목
    /// </summary>
    [MaxLength(255)]
    public string? SongTitle { get; set; }

    /// <summary>
    /// 현재까지 누적된 총 포인트(치즈)
    /// </summary>
    public int CurrentPoints { get; set; }

    /// <summary>
    /// 마지막으로 후원(누적)한 시청자의 닉네임
    /// </summary>
    [MaxLength(100)]
    public string? LastDonatorName { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
