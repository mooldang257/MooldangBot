using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [마스터 데이터]: 스트리머나 시청자가 입력한 미검증 노래 데이터입니다.
/// 관리자가 검토 후 Master_SongLibrary로 승격시킵니다.
/// </summary>
public class Master_SongStaging
{
    [Key]
    public int Id { get; set; }

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
    /// 입력 시 사용된 가칭 또는 태그
    /// </summary>
    [MaxLength(500)]
    public string? Alias { get; set; }

    [MaxLength(500)]
    public string? YoutubeUrl { get; set; }

    [MaxLength(500)]
    public string? YoutubeTitle { get; set; }

    [MaxLength(1000)]
    public string? LyricsUrl { get; set; }

    /// <summary>
    /// 데이터 출처 (Streamer, Viewer 등)
    /// </summary>
    [Required]
    public MetadataSourceType SourceType { get; set; }

    /// <summary>
    /// 등록자 식별자 (StreamerId 또는 ViewerUID 등)
    /// </summary>
    [MaxLength(100)]
    public string? SourceId { get; set; }

    /// <summary>
    /// [v11.7] 의미 기반 검색을 위한 벡터 데이터 (MariaDB 11.7 신기능)
    /// </summary>
    [NotMapped]
    public float[]? TitleVector { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
}
