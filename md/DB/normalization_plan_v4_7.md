🛠️ Philosophy & System 정규화 엔티티 설계안 (참고용)
1. BroadcastSession (오시리스의 기록관)

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

[Index(nameof(StreamerProfileId), nameof(IsActive))]
public class BroadcastSession
{
    [Key]
    public int Id { get; set; }

    // [정규화] ChzzkUid -> StreamerProfileId
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public KstClock StartTime { get; set; }
    public KstClock? EndTime { get; set; }
    public KstClock LastHeartbeatAt { get; set; }

    public int TotalChatCount { get; set; }
    public string? TopKeywordsJson { get; set; } 
    public string? TopEmotesJson { get; set; }   
    public bool IsActive { get; set; } = true;
}
2. IamfStreamerSetting (IAMF 스트리머 설정)

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

public class IamfStreamerSetting
{
    // [정규화] PK이자 FK로 활용하여 완벽한 1:1 관계 구성
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public bool IsIamfEnabled { get; set; } = true;
    public bool IsVisualResonanceEnabled { get; set; } = true;
    public bool IsPersonaChatEnabled { get; set; } = true;
    public double SensitivityMultiplier { get; set; } = 1.0;
    public double OverlayOpacity { get; set; } = 0.5;
    public KstClock LastUpdatedAt { get; set; } = KstClock.Now;
}
3. IamfVibrationLog & StreamerKnowledge

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities.Philosophy;

[Index(nameof(StreamerProfileId), nameof(CreatedAt))]
public class IamfVibrationLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    public double RawHz { get; set; }                    
    public double EmaHz { get; set; }                    
    public double StabilityScore { get; set; }           
    public KstClock CreatedAt { get; set; } = KstClock.Now;
}

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
    public string Keyword { get; set; } = string.Empty;    

    [Required]
    public string IntentAnswer { get; set; } = string.Empty; 

    public bool IsActive { get; set; } = true;
    public KstClock CreatedAt { get; set; } = KstClock.Now;
}