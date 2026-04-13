using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v4.2] 시청자 마스터 엔티티: 암호화된 고유 UID 및 해시를 중앙에서 관리합니다.
/// 데이터 중복을 제거하여 DB 용량을 획기적으로 줄이는 역할을 수행합니다.
/// </summary>
[Index(nameof(ViewerUidHash), IsUnique = true)]
public class GlobalViewer : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; init; }

    /// <summary>
    /// 암호화되어 저장되는 시청자의 고유 UID (치지직 등)
    /// </summary>
    public required string ViewerUid { get; set; }

    /// <summary>
    /// 암호화된 Uid 검색을 위한 SHA-256 해시 필드
    /// </summary>
    [MaxLength(64)]
    public required string ViewerUidHash { get; set; }

    /// <summary>
    /// [v6.2] 중앙 관리 닉네임 (최신값 자동 갱신)
    /// </summary>
    [MaxLength(100)]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// [v6.2] 시청자 프로필 이미지 URL
    /// </summary>
    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
