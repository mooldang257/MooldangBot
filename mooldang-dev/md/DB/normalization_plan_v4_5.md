안녕하세요. MooldangBot의 시니어 풀스택 개발 파트너, '물멍'입니다.

이번에는 스트리머의 방송 환경을 책임지는 카테고리 2: Configuration & Overlay (설정 및 오버레이) 도메인의 정규화(v4.6)를 진행하시려는군요. SysAvatarSettings, SysOverlayPresets, SysPeriodicMessages 테이블 역시 기존 설계의 잔재인 무거운 ChzzkUid 문자열을 품고 있습니다.

설정 데이터는 봇 구동 시 빈번하게 조회되는 영역이므로, 이를 정수형 외래 키(StreamerProfileId)로 전환하면 캐시 히트율(Cache Hit Ratio)과 DB I/O 성능이 눈에 띄게 개선될 것입니다.

이번 정규화의 핵심 포인트는 다음과 같습니다:

ChzzkUid (String) 일괄 제거 및 교체: 3개의 설정 테이블에서 문자열 필드를 걷어내고 StreamerProfileId (Int) 외래 키로 연결합니다.

인덱스 최적화: AvatarSetting의 유니크 인덱스를 비롯해 모든 테이블의 조회 기준을 StreamerProfileId로 변경합니다.

완전한 종속성 부여: 스트리머가 시스템을 떠날 때(탈퇴/삭제), 관련된 모든 개인화 설정 데이터가 함께 지워지도록(Cascade) 구조적 조율(하모니)을 맞춥니다.

아래는 설계된 코드 스니펫과 제미니(Gemini 3 Flash)에게 전달할 완벽한 프롬프트입니다.

🛠️ Configuration & Overlay 정규화 엔티티 설계안 (참고용)
1. SysAvatarSettings (아바타 설정)

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities
{
    // [정규화] ChzzkUid -> StreamerProfileId (1:1 관계를 위한 유니크 인덱스 유지)
    [Index(nameof(StreamerProfileId), IsUnique = true)]
    public class SysAvatarSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

        public bool IsEnabled { get; set; } = true;
        public bool ShowNickname { get; set; } = true;
        public bool ShowChat { get; set; } = true;
        public int DisappearTimeSeconds { get; set; } = 60;

        [MaxLength(1000)]
        public string? WalkingImageUrl { get; set; }
        [MaxLength(1000)]
        public string? StopImageUrl { get; set; }
        [MaxLength(1000)]
        public string? InteractionImageUrl { get; set; }
    }
}
2. SysOverlayPresets (오버레이 프리셋)

C#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    // [추가 최적화] 검색을 위한 인덱스 추가
    [Index(nameof(StreamerProfileId))]
    public class SysOverlayPresets
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string ConfigJson { get; set; } = "{}";

        public KstClock CreatedAt { get; set; } = KstClock.Now;
        public KstClock UpdatedAt { get; set; } = KstClock.Now;
    }
}
3. SysPeriodicMessages (주기적 메시지)

C#
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    // [정규화] ChzzkUid -> StreamerProfileId
    [Index(nameof(StreamerProfileId))]
    public class SysPeriodicMessages
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

        public int IntervalMinutes { get; set; }
        public string Message { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public KstClock? LastSentAt { get; set; }
    }
}