using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v7.0] 시청자 무료 포인트 지갑: 채팅 등으로 얻는 비유료 포인트를 관리합니다. 
/// 초고빈도 쓰기 성능을 위해 인덱스를 최소화하고 Write-Back 패턴으로 동기화됩니다.
/// </summary>
[Table("func_viewer_points")]
[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
public class ViewerPoint : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual GlobalViewer? GlobalViewer { get; set; }

    /// <summary>
    /// 현재 보유 포인트 (Write-Back에 의해 MariaDB에 최종 반영되는 값)
    /// </summary>
    public int Points { get; set; } = 0;

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
