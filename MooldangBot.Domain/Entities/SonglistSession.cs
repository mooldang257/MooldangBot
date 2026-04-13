using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v4.5 정문화]: 스트리머별 송리스트 세션 정보를 관리합니다.
/// [v6.1] 정규화: ISoftDeletable, IAuditable 인터페이스를 적용합니다.
/// </summary>
public class SonglistSession : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public KstClock StartedAt { get; set; }
    
    public KstClock? EndedAt { get; set; }

    public int RequestCount { get; set; } = 0;

    public int CompleteCount { get; set; } = 0;

    // [v6.1] 정규화: IsActive -> IsDeleted 전환
    public bool IsActive { get; set; } = true; // [v6.1.5] 기능 활성화

    public bool IsDeleted { get; set; } = false; // [v6.1.5] 존재 거버넌스
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
