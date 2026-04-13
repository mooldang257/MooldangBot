using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [천상의 장부]: 포인트의 모든 유입과 유출을 기록하는 세부 거래 내역서입니다. (Retention: 30 Days)
/// </summary>
[Index(nameof(StreamerProfileId), nameof(CreatedAt))]
[Index(nameof(GlobalViewerId), nameof(CreatedAt))]
public class PointTransactionHistory
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }
    
    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual GlobalViewer? GlobalViewer { get; set; }

    public int Amount { get; set; }                    // 증감액 (+는 획득, -는 소비)
    public int BalanceAfter { get; set; }              // 거래 후 잔액
    
    public PointTransactionType Type { get; set; }     // 거래 구분 (Earn, Spend, etc.)
    
    [MaxLength(200)]
    public string? Reason { get; set; }                // 사유 (예: "채팅 보너스", "룰렛:황금열쇠")

    public KstClock CreatedAt { get; set; } = KstClock.Now;
}
