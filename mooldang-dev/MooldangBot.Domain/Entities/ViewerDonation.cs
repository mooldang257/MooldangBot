using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v7.0] 시청자 유료 재화 지갑: 치즈나 유료 별사탕 등 실제 현금 가치가 있는 재화를 관리합니다.
/// 절대적인 무결성을 위해 낙관적 락(RowVersion) 및 동기식 DB 반영을 원칙으로 합니다.
/// </summary>
[Table("func_viewer_donations")]
[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
public class ViewerDonation : IAuditable
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
    /// 현재 보유 유료 재화 잔액
    /// </summary>
    public int Balance { get; set; } = 0;

    /// <summary>
    /// 누적 후원 총액
    /// </summary>
    public long TotalDonated { get; set; } = 0;

    /// <summary>
    /// [v7.0] 낙관적 락을 위한 행 버전 (무결성 수호)
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
