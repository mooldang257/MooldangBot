using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [오시리스의 기록관]: 방송 세션 내에서 발생하는 제목 및 카테고리 변화의 궤적을 기록하는 엔티티입니다.
/// </summary>
public class BroadcastHistoryLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BroadcastSessionId { get; set; }

    [ForeignKey(nameof(BroadcastSessionId))]
    public virtual BroadcastSession? BroadcastSession { get; set; }

    [MaxLength(255)]
    public string? Title { get; set; }

    [MaxLength(100)]
    public string? CategoryName { get; set; }

    [Required]
    public KstClock LogDate { get; set; } = KstClock.Now;
}
