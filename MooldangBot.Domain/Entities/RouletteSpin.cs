using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [오시리스의 영속성]: 룰렛 실행 결과 및 전송 스케줄을 영구 저장하는 엔티티입니다. (v1.9.9)
/// 11시간 이상의 장기 룰렛 세션에서도 결과 유실을 방지하기 위해 사용됩니다.
/// </summary>
public class RouletteSpin
{
    [Key]
    public string Id { get; set; } = string.Empty; // GUID

    [Required]
    [MaxLength(50)]
    public string ChzzkUid { get; set; } = string.Empty;

    public int RouletteId { get; set; }

    [MaxLength(50)]
    public string ViewerUid { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ViewerNickname { get; set; } = string.Empty;

    [Required]
    public string ResultsJson { get; set; } = string.Empty; // List<string> 직렬화

    public string Summary { get; set; } = string.Empty;

    public bool IsCompleted { get; set; } = false;

    [Required]
    public KstClock ScheduledTime { get; set; } // 결과 채팅이 전송되어야 할 예정 시각

    public KstClock CreatedAt { get; set; } = KstClock.Now; // KST
}
