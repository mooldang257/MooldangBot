using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v19.0 고도화]: 스트리머가 정성껏 관리하는 '완성형' 노래책 엔터티입니다.
/// 마스터 라이브러리 정보를 스냅샷으로 가지며, 스트리머 특화 퍼포먼스 데이터를 포함합니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(SongNo), IsUnique = true)]
public class FuncSongBooks : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// [v19.1] 스트리머별 고유 곡 번호 (1부터 시작, 엑셀 관리용)
    /// </summary>
    public int SongNo { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    /// <summary>
    /// 마스터 라이브러리와의 연결고리 (독립적 동작을 위해 필수값은 아님)
    /// </summary>
    public long? SongLibraryId { get; set; }

    // [기본 정보 - 스냅샷]
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Artist { get; set; }

    [MaxLength(255)]
    public string? Album { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    // [스트리머 퍼포먼스 - 특화 데이터]
    [MaxLength(50)]
    public string? Pitch { get; set; }        // 예: -2, 원키, +1

    [MaxLength(50)]
    public string? Proficiency { get; set; }  // 예: 완창, 1절, 연습중

    // [미디어 리소스 URL - 스냅샷 및 공용 경로]
    [MaxLength(1000)]
    public string? ThumbnailUrl { get; set; } // 원본 썸네일 URL

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; } // 공용 저장소 내부 경로

    [MaxLength(1000)]
    public string? MrUrl { get; set; }

    [MaxLength(1000)]
    public string? LyricsUrl { get; set; }

    [MaxLength(1000)]
    public string? ReferenceUrl { get; set; } // 참고용 유튜브 링크 등

    // [방송 통계 및 제어]
    public bool IsRequestable { get; set; } = true;

    public int SingCount { get; set; } = 0; // 기존 UsageCount를 계승

    /// <summary>
    /// 곡 신청에 필요한 최소 포인트(치즈). 0이면 무료 신청 가능.
    /// </summary>
    public int RequiredPoints { get; set; } = 0;

    public DateTime? LastSungAt { get; set; }

    // [지능형 검색 필드 - 스냅샷]
    [MaxLength(500)]
    public string? Alias { get; set; }

    [MaxLength(200)]
    public string? TitleChosung { get; set; }

    [NotMapped]
    public float[]? TitleVector { get; set; }

    // [시스템 거버넌스]
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
