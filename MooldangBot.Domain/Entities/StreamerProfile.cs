using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

[Index(nameof(ChzzkUid), IsUnique = true)]
public class StreamerProfile : ISoftDeletable, IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        // ⭐ 치지직 프로필 닉네임 저장 필드 추가
        [MaxLength(100)]
        public string? ChannelName { get; set; }

        // ⭐ 치지직 프로필 이미지 폴더 등 보관 필드 추가
        [MaxLength(500)]
        public string? ProfileImageUrl { get; set; }

        // ⭐ 치지직 공식 OAuth 인증 토큰 저장소 추가
        public string? ChzzkAccessToken { get; set; }
        public string? ChzzkRefreshToken { get; set; }
        public KstClock? TokenExpiresAt { get; set; }

        // [추가] 2. 공지 메모
        public string? NoticeMemo { get; set; }

        // [추가] 4. 화면 세부 디자인 설정 (복잡한 컴포넌트 설정은 JSON으로 통째로 저장)
        public string? DesignSettingsJson { get; set; }

        // [추가] 5. 시청자 포인트 및 출석 설정
        public int PointPerChat { get; set; } = 1;
        // [v6.2.1] 후원 잔액 시스템 설정
        public bool IsAutoAccumulateDonation { get; set; } = false;

        // [v6.1.6] 통합: IsBotEnabled와 IsActive는 동일한 기능으로 간주하여 IsActive로 단일화합니다.
        public bool IsActive { get; set; } = false; // [v6.1.5] 스트리머의 봇 사용 여부 (Default:Off)

        public bool IsDeleted { get; set; } = false; // [v6.1.5] 채널 데이터 존재 상태 (복구용)
        public KstClock? DeletedAt { get; set; }

        public bool IsMasterEnabled { get; set; } = true; // [v6.1.6] 관리자 마스터 킬 스위치 (Default:On)

        public KstClock CreatedAt { get; set; } = KstClock.Now;
        public KstClock? UpdatedAt { get; set; }

        public int? ActiveOverlayPresetId { get; set; }
    }