using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [하모니의 성대]: 스트리머가 업로드하거나 녹음한 사운드 자원을 관리하는 엔티티입니다.
/// </summary>
public class FuncSoundAssets : IAuditable
{
    [Key]
    public int Id { get; set; }

    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string SoundUrl { get; set; } = string.Empty;

    /// <summary>
    /// 자산 유형: "Recording" (직접 녹음), "Upload" (파일 업로드)
    /// </summary>
    [MaxLength(20)]
    public string AssetType { get; set; } = "Upload";

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
