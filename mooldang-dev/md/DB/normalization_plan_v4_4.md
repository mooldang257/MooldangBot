이번 정규화의 핵심 조율(하모니) 포인트는 다음과 같습니다:

ChzzkUid (String) 일괄 제거: 모든 노래 관련 테이블에서 문자열을 걷어내고 StreamerProfileId (Int) 외래 키로 연결합니다.

신청자 추적 (선택적 확장): 현재 SongQueue에는 누가 곡을 신청했는지 기록하는 필드가 없습니다. v4.2에서 구축한 GlobalViewerId (Int)를 Nullable로 추가하여, 추후 누가 어떤 곡을 신청했는지 추적할 수 있는 기반을 마련하는 것을 추천합니다.

인덱스 재정렬: MariaDB 환경에 맞춰 복합 인덱스의 기준을 StreamerProfileId로 변경하여 조회 성능을 극대화합니다.

아래는 설계된 코드 스니펫과 제미니(Gemini 3 Flash)에게 전달할 완벽한 프롬프트입니다.

🛠️ 노래 도메인 정규화 엔티티 설계안 (참고용 스니펫)
1. FuncSongBooks (노래장)

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

[Index(nameof(StreamerProfileId), nameof(Id))]
public class FuncSongBooks
{
    [Key]
    public int Id { get; set; }

    // [정규화] ChzzkUid -> StreamerProfileId
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Artist { get; set; }

    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; } = 0;
    public KstClock CreatedAt { get; set; } = KstClock.Now;
    public KstClock UpdatedAt { get; set; } = KstClock.Now;
}
2. FuncSongListQueues (노래 대기열)

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

[Index(nameof(StreamerProfileId), nameof(Id))]
[Index(nameof(StreamerProfileId), nameof(Status), nameof(CreatedAt))]
public class FuncSongListQueues
{
    [Key]
    public int Id { get; set; }

    // [정규화] ChzzkUid -> StreamerProfileId
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    // [확장 제안] 누가 신청했는지 추적하기 위한 CoreGlobalViewers 연결
    public int? GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual CoreGlobalViewers? CoreGlobalViewers { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty; 

    [MaxLength(100)]
    public string? Artist { get; set; } 

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; 

    public int SortOrder { get; set; } = 0; 
    public KstClock CreatedAt { get; set; } = KstClock.Now; 
}