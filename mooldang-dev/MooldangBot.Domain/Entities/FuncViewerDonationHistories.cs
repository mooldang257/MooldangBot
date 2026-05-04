using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [v7.0] 유료 재화 트랜잭션 로그: 치즈 등의 모든 변동 내역을 스냅샷과 함께 기록합니다.
/// 플랫폼 대사(Reconciliation) 및 사후 감사용 핵심 데이터입니다.
/// </summary>
[Index(nameof(StreamerProfileId))]
[Index(nameof(GlobalViewerId))]
[Index(nameof(PlatformTransactionId), IsUnique = true)]
public class FuncViewerDonationHistories : IAuditable
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual CoreGlobalViewers? CoreGlobalViewers { get; set; }

    /// <summary>
    /// [핵심] 치지직 API 등 플랫폼에서 발급한 고유 트랜잭션 ID
    /// </summary>
    [MaxLength(100)]
    public required string PlatformTransactionId { get; set; }

    /// <summary>
    /// 변동된 금액 (+/-)
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// [핵심] 트랜잭션 직후의 잔액 스냅샷 (무결성 검증용)
    /// </summary>
    public int BalanceAfter { get; set; }

    /// <summary>
    /// 트랜잭션 타입 (DONATION, REFUND, MANUAL 등)
    /// </summary>
    [MaxLength(20)]
    public required string TransactionType { get; set; }

    /// <summary>
    /// [핵심] 유동적인 데이터(메시지 내용, TTS 정보 등) 저장을 위한 JSON 필드
    /// </summary>
    [Column(TypeName = "json")]
    public string? Metadata { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
