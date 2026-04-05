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

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
