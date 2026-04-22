using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [스트리머 전용]: 스트리머가 수동으로 입력한 곡의 정보(링크, 가사 등)를 보관하는 테이블입니다.
/// MetadataKey를 통해 Master_SongLibrary와 느슨하게 연결됩니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(SongLibraryId), IsUnique = true)]
public class Streamer_SongLibrary : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    /// <summary>
    /// [v13.1] Snowflake 알고리즘 기반의 전역 유일 식별자입니다.
    /// </summary>
    [Required]
    public long SongLibraryId { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Artist { get; set; }

    [MaxLength(500)]
    public string? YoutubeUrl { get; set; }

    [MaxLength(500)]
    public string? YoutubeTitle { get; set; }

    [Column(TypeName = "TEXT")]
    public string? Lyrics { get; set; }

    /// <summary>
    /// [v18.0] 검색용 별칭 및 줄임말 (예: "우언죽")
    /// </summary>
    [MaxLength(500)]
    public string? Alias { get; set; }

    /// <summary>
    /// [v18.0] 제목 초성 (예: "ㅇㄹㄴㅇㅈㄱ")
    /// </summary>
    [MaxLength(200)]
    public string? TitleChosung { get; set; }

    /// <summary>
    /// [v11.7] 의미 기반 검색을 위한 벡터 데이터 (MariaDB 11.7 전용)
    /// </summary>
    [Column(TypeName = "VECTOR(768)")]
    public byte[]? TitleVector { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
