using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [오시리스의 영속]: 전역 음악 메타데이터 및 벡터 데이터를 저장하는 테이블입니다.
/// 모든 스트리머가 공유하는 지능형 음악 지식 베이스의 핵심 엔티티입니다.
/// </summary>
[Table("GlobalMusicMetadata")]
public class GlobalMusicMetadata
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string NormalizedArtist { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string NormalizedTitle { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(500)]
    public string? LyricsUrl { get; set; }

    /// <summary>
    /// BGE-M3 모델로 생성된 1024차원 벡터 데이터
    /// </summary>
    [Required]
    public float[] SearchVector { get; set; } = Array.Empty<float>();

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
