using System;
using System.ComponentModel.DataAnnotations;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [공용 자산]: 여러 스트리머가 공유하는 앨범 아트 및 썸네일 정보입니다.
/// MooldangBot_Common 스키마에서 관리됩니다.
/// </summary>
public class CommonThumbnail : IAuditable
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 파일 내용의 SHA-256 해시값 (중복 저장 방지용)
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// 검색을 위한 아티스트명
    /// </summary>
    [MaxLength(200)]
    public string? Artist { get; set; }

    /// <summary>
    /// 검색을 위한 곡 제목
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// 1.8TB 디스크 내 실제 파일 경로 (상대 경로)
    /// 예: /songbook/thumbnails/Ado_Gyakko_hash.webp
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// 원본 수집 URL (iTunes, YouTube 등)
    /// </summary>
    [MaxLength(1000)]
    public string? SourceUrl { get; set; }

    /// <summary>
    /// 현재 이 이미지를 참조하고 있는 총 곡 수
    /// </summary>
    public int ReferenceCount { get; set; } = 1;

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
