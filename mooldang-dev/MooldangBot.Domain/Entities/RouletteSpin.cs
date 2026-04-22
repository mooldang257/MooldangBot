using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [오시리스의 영속성]: 룰렛 실행 결과 및 전송 스케줄을 영구 저장하는 엔티티입니다. (v1.9.9)
/// 11시간 이상의 장기 룰렛 세션에서도 결과 유실을 방지하기 위해 사용됩니다.
/// </summary>
public class RouletteSpin
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public int StreamerProfileId { get; set; } // [v4.4] ChzzkUid 제거 및 FK 도입

    public int RouletteId { get; set; }

    public int GlobalViewerId { get; set; } // [v4.4] ViewerUid 제거 및 FK 도입

    [Required]
    public string ResultsJson { get; set; } = string.Empty; // List<string> 직렬화

    public string Summary { get; set; } = string.Empty;

    public bool IsCompleted { get; set; } = false;

    [Required]
    public KstClock ScheduledTime { get; set; } // 결과 채팅이 전송되어야 할 예정 시각

    public KstClock CreatedAt { get; set; } = KstClock.Now; // KST

    // Navigation Properties
    [JsonIgnore]
    public StreamerProfile? StreamerProfile { get; set; }

    [JsonIgnore]
    public GlobalViewer? GlobalViewer { get; set; }
}
