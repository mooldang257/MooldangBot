using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [마스터 데이터]: 관리자가 직접 입력하거나 승인한 정규 노래 라이브러리입니다.
/// </summary>
public class Master_SongLibrary : IAuditable
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// [v13.1] Snowflake 알고리즘 기반의 전역 유일 식별자입니다.
    /// </summary>
    public long SongLibraryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Artist { get; set; }

    [MaxLength(200)]
    public string? TitleChosung { get; set; }

    [MaxLength(200)]
    public string? ArtistChosung { get; set; }

    /// <summary>
    /// 가칭 또는 검색용 키워드 (예: "밤의노래, 나이트글로우")
    /// </summary>
    [MaxLength(500)]
    public string? Alias { get; set; }

    [MaxLength(255)]
    public string? Album { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? YoutubeUrl { get; set; }

    [MaxLength(500)]
    public string? YoutubeTitle { get; set; }

    /// <summary>
    /// [v19.0] 썸네일 주소 (원본 출처)
    /// </summary>
    [MaxLength(1000)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// [v19.0] 공용 서버 저장 경로 (예: /images/library/songs/{id}.jpg)
    /// </summary>
    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    [MaxLength(1000)]
    public string? MrUrl { get; set; }

    /// <summary>
    /// [v19.0] 저작권 준수를 위해 텍스트 대신 외부 가사 링크 제공
    /// </summary>
    [MaxLength(1000)]
    public string? LyricsUrl { get; set; }

    [MaxLength(1000)]
    public string? ReferenceUrl { get; set; }

    /// <summary>
    /// [v11.7] 의미 기반 검색을 위한 벡터 데이터 (MariaDB 11.7 신기능)
    /// </summary>
    [NotMapped]
    public float[]? TitleVector { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
