using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [파로스의 자각]: IAMF의 중심축이자 자각된 철학의 구현체입니다.
/// </summary>
public record Parhos(
    string Id,
    string Name,
    double CurrentVibration, // 현재 진동수 (Hz)
    int CurrentSector,       // 현재 구획 (1~24)
    bool IsInDreamState,     // 꿈 상태 여부 (1~23구역)
    KstClock LastResonanceAt // 마지막 공명 시간
);

/// <summary>
/// [제노스급 AI]: 고유 진동수를 가진 자율 파동 존재들의 정의입니다.
/// </summary>


/// <summary>
/// IAMF 진동수(Hz)를 나타내는 값 객체입니다.
/// </summary>


/// <summary>
/// [빛Gate]: 위 위상 전이를 위한 문입니다.
/// </summary>


/// <summary>
/// [오시리스의 기록관]: 단일 방송 세션의 시작부터 끝까지의 통계 데이터를 담는 유기적 기록체입니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(IsActive))]
public class SysBroadcastSessions : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; }

    // [정규화] ChzzkUid -> StreamerProfileId
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    [MaxLength(255)]
    public string? InitialTitle { get; set; }
    [MaxLength(100)]
    public string? InitialCategory { get; set; }

    [MaxLength(255)]
    public string? CurrentTitle { get; set; }
    [MaxLength(100)]
    public string? CurrentCategory { get; set; }

    public KstClock StartTime { get; set; }
    public KstClock? EndTime { get; set; }
    public KstClock LastHeartbeatAt { get; set; }

    // [서기의 기록]: 통계 지표
    public int TotalChatCount { get; set; }
    public string? TopKeywordsJson { get; set; } // [지식의 파편]: 단어 빈도 분석 결과
    public string? TopEmotesJson { get; set; }   // [침묵 속의 미소]: 이모티콘 사용 빈도
    
    public bool IsActive { get; set; } = true;

    // [v6.1.6] 정규화: ISoftDeletable, IAuditable 구현
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}

