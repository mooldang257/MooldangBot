using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v4.5 정문화]: 스트리머별 고유 신청곡 목록(송리스트)을 관리하는 엔터티입니다.
/// [v6.1] 정규화: ISoftDeletable, IAuditable 상속 및 필드 타입 최적화.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(Id))]
public class SongBook : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Artist { get; set; }

    public string? Genre { get; set; }

    public int UsageCount { get; set; } = 0;

    // [v6.1] 정규화: IsActive -> IsDeleted 전환
    public bool IsActive { get; set; } = true; // [v6.1.5] 기능 활성화

    public bool IsDeleted { get; set; } = false; // [v6.1.5] 존재 거버넌스
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
