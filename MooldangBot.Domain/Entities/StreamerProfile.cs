using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

[Index(nameof(ChzzkUid), IsUnique = true)]
public class StreamerProfile : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; init; }

    [MaxLength(50)]
    public required string ChzzkUid { get; set; }

    [MaxLength(100)]
    public string? ChannelName { get; set; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// [물멍]: 선장님이 직접 명명하신 함교의 고유 주소(Slug)입니다.
    /// </summary>
    [MaxLength(50)]
    public string? Slug { get; set; }

    public string? ChzzkAccessToken { get; set; }
    public string? ChzzkRefreshToken { get; set; }
    public KstClock? TokenExpiresAt { get; set; }

    public string? NoticeMemo { get; set; }

    public string? DesignSettingsJson { get; set; }

    public int PointPerChat { get; set; } = 1;

    public bool IsAutoAccumulateDonation { get; set; } = false;
    public int PointPerDonation1000 { get; set; } = 1000;

    public bool IsActive { get; set; } = false;

    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public bool IsMasterEnabled { get; set; } = true;

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }

    public int? ActiveOverlayPresetId { get; set; }

    public int OverlayTokenVersion { get; set; } = 1;

    [MaxLength(32)]
    public string? OverlayToken { get; set; }
}