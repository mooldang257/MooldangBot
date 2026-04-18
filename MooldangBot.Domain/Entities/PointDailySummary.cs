using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [천상의 장부]: 일자별 포인트 유통량을 요약 집계한 테이블입니다.
/// (P1: 효율성): 수백만 건의 로그 대신 이 테이블을 조회하여 대시보드 성능을 확보합니다.
/// </summary>
[Table("point_daily_summaries")]
[Index(nameof(StreamerProfileId), nameof(Date), IsUnique = true)]
public class PointDailySummary
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    public DateTime Date { get; set; }                // 집계 대상 일자 (00:00:00)

    public long TotalEarned { get; set; }              // 총 획득 포인트 (+)
    public long TotalSpent { get; set; }               // 총 사용 포인트 (-)
    
    public int UniqueViewerCount { get; set; }         // 해당 일자 활동 시청자 수
    
    [MaxLength(1000)]
    public string? TopCommandStatsJson { get; set; }   // 상위 명령어 사용량 (v11.1 추가)

    public KstClock LastUpdatedAt { get; set; } = KstClock.Now;
}
