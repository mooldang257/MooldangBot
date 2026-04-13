using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [세피로스의 기록]: 명령어 실행 이력을 추적하는 엔티티입니다. (Retention: 30 Days)
/// </summary>
[Table("log_command_executions")]
[Index(nameof(StreamerProfileId), nameof(CreatedAt))]
[Index(nameof(Keyword), nameof(CreatedAt))]
public class CommandExecutionLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    [MaxLength(100)]
    public string Keyword { get; set; } = string.Empty;

    public int GlobalViewerId { get; set; }
    
    [ForeignKey(nameof(GlobalViewerId))]
    public virtual GlobalViewer? GlobalViewer { get; set; }

    public bool IsSuccess { get; set; }
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public int DonationAmount { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
}
