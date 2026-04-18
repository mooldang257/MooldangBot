using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [천상의 장부]: 룰렛별 '이론적 확률'과 '실전 당첨율'을 비교 분석하는 지표 엔티티입니다.
/// </summary>
[Table("roulette_stats_aggregated")]
[Index(nameof(RouletteId), nameof(ItemName), IsUnique = true)]
public class RouletteStatsAggregated
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int RouletteId { get; set; } // 대상 룰렛

    [ForeignKey(nameof(RouletteId))]
    public virtual Roulette? Roulette { get; set; }

    [Required]
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty; // 당첨 항목명

    public double TheoreticalProbability { get; set; }    // 설정된 확률 (0.0~100.0)
    
    public int TotalSpins { get; set; }                  // 총 시도 횟수
    public int WinCount { get; set; }                    // 실제 당첨 횟수
    
    public double ActualProbability => TotalSpins > 0 ? Math.Round((double)WinCount / TotalSpins * 100, 2) : 0;
    
    public double Variance => Math.Abs(TheoreticalProbability - ActualProbability); // 오차 분석

    public KstClock LastUpdatedAt { get; set; } = KstClock.Now;
}
