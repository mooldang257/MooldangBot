using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

/// <summary>
/// [피닉스의 기록]: iamf_scenarios 테이블과 매핑되는 엔티티입니다.
/// </summary>
public class IamfScenario : ISoftDeletable, IAuditable
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 추가

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    [MaxLength(100)]
    public string ScenarioId { get; set; } = string.Empty;

    public int Level { get; set; } = 1;

    [Required]
    public string Content { get; set; } = string.Empty;

    public double VibrationHz { get; set; }

    public bool IsActive { get; set; } = true; // [v6.1.5] 시나리오 활성화 (토글용)

    // [v6.1] 정규화: ISoftDeletable, IAuditable 구현
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}

/// <summary>
/// [제노스급 AI 등록부]: iamf_genos_registry 테이블과 매핑되는 엔티티입니다.
/// </summary>
public class IamfGenosRegistry : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; } // [v4.9] 정규화된 PK

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 추가

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public double Frequency { get; set; }

    [Required]
    [MaxLength(200)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Metaphor { get; set; }

    public KstClock LastSyncAt { get; set; } = KstClock.Now;

    public bool IsActive { get; set; } = true; // [v6.1.5] AI 페르소나 활성화 (토글용)

    // [v6.1] 정규화: ISoftDeletable, IAuditable 구현
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}

/// <summary>
/// [파로스의 윤회 이력]: iamf_parhos_cycles 테이블과 매핑되는 엔티티입니다.
/// </summary>
public class IamfParhosCycle : ISoftDeletable, IAuditable
{
    [Key]
    public int Id { get; set; } // [v4.9] 정규화된 PK

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 부여

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public int CycleId { get; set; } // 해당 채널의 몇 번째 사이클인가 (1, 2, 3...)

    public double VibrationAtDeath { get; set; }

    public int RebirthPercentage { get; set; }

    // [v6.1] 정규화: ISoftDeletable, IAuditable 구현
    public bool IsDeleted { get; set; } = false;
    public KstClock? DeletedAt { get; set; }

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}

/// <summary>
/// [피닉스의 눈금]: 시스템 진동수의 미세한 변화와 안정도를 기록하는 시계열 엔티티입니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(CreatedAt))]
public class IamfVibrationLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public double RawHz { get; set; }                    // 실제 계산된 진동수

    public double EmaHz { get; set; }                    // 지수 이동 평균 (추세)

    public double StabilityScore { get; set; }           // 공명 안정도 (0.0~1.0)

    public KstClock CreatedAt { get; set; } = KstClock.Now;
}

public class IamfStreamerSetting
{
    // [정규화] PK이자 FK로 활용하여 완벽한 1:1 관계 구성
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public bool IsIamfEnabled { get; set; } = true;

    /// <summary>
    /// [스트리머의 통제권 확장]: 시각적 오버레이 공명 활성화 여부
    /// </summary>
    public bool IsVisualResonanceEnabled { get; set; } = true;

    /// <summary>
    /// [스트리머의 통제권 확장]: AI 페르소나 채팅 활성화 여부
    /// </summary>
    public bool IsPersonaChatEnabled { get; set; } = true;

    public double SensitivityMultiplier { get; set; } = 1.0;

    public double OverlayOpacity { get; set; } = 0.5;

    public KstClock LastUpdatedAt { get; set; } = KstClock.Now;
}

/// <summary>
/// [주인의 목소리]: 스트리머가 봇에게 주입한 특정 상황에 대한 '의도된 지식'을 저장합니다.
/// </summary>
[Index(nameof(StreamerProfileId), nameof(Keyword))]
public class StreamerKnowledge
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    [MaxLength(100)]
    public string Keyword { get; set; } = string.Empty;    // 감지할 핵심어 [대변인의 방패]

    [Required]
    public string IntentAnswer { get; set; } = string.Empty; // 스트리머가 의도한 정답

    public bool IsActive { get; set; } = true; // [v6.1.5] 기능 활성화 (삭제는 프로필 종속)

    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock? UpdatedAt { get; set; }
}
