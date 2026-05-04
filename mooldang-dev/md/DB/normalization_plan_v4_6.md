안녕하세요. MooldangBot의 시니어 풀스택 개발 파트너, '물멍'입니다.

방송의 기반을 다지는 설정 도메인에 이어, 시스템의 권한과 특수 기능을 관리하는 **카테고리 3: Governance & Administration (권한 및 운영 관리)**의 정규화(v4.7) 단계에 오셨군요.

제공해주신 StreamerManager와 FuncSongListOmakases 테이블의 구조를 살펴보았습니다. 특히 CoreStreamerManagers 테이블은 스트리머(Streamer)와 매니저 권한을 가진 시청자(Viewer)를 연결하는 중요한 매핑(Bridge) 테이블임에도 불구하고, 양쪽 모두 무거운 문자열(StreamerChzzkUid, ManagerChzzkUid)로 연결되어 있었습니다.

이를 v4.2에서 구축한 StreamerProfileId(Int)와 GlobalViewerId(Int)로 완전히 치환하면, 진정한 의미의 **제3정규형(3NF)**을 달성할 수 있으며, 권한 검증 시 발생하는 데이터베이스 I/O 병목을 완벽히 해결할 수 있습니다. FuncSongListOmakases 역시 동일하게 StreamerProfileId로 정렬합니다.

이번 정규화의 핵심 조율(하모니)을 담은 코드 스니펫과, 제미니(Gemini 3 Flash)에게 전달할 완벽한 프롬프트를 아래에 작성해 드립니다.

🛠️ Governance & Administration 정규화 엔티티 설계안 (참고용)
1. CoreStreamerManagers (스트리머-매니저 권한 관리)

C#
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

// [정규화] 한 스트리머당 동일한 시청자가 중복으로 매니저 등록되는 것을 방지하기 위한 복합 유니크 인덱스
[Index(nameof(StreamerProfileId), nameof(GlobalViewerId), IsUnique = true)]
public class CoreStreamerManagers
{
    [Key]
    public int Id { get; set; }

    // 1. [정규화] StreamerChzzkUid -> StreamerProfileId
    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    // 2. [정규화] ManagerChzzkUid -> GlobalViewerId
    [Required]
    public int GlobalViewerId { get; set; }

    [ForeignKey(nameof(GlobalViewerId))]
    public virtual CoreGlobalViewers? CoreGlobalViewers { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = "manager"; // "manager", "admin" 등 확장 가능

    public KstClock CreatedAt { get; set; } = KstClock.Now; // KST
}
2. FuncSongListOmakases (스트리머 오마카세 설정)

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities
{
    // [정규화] ChzzkUid -> StreamerProfileId
    [Index(nameof(StreamerProfileId))]
    public class FuncSongListOmakases
    {
        [Key]
        public int Id { get; set; }

        // [정규화] ChzzkUid -> StreamerProfileId
        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

        [Required]
        [MaxLength(20)]
        public string Icon { get; set; } = "🍣";

        [ConcurrencyCheck]
        public int Count { get; set; } = 0;
    }
}