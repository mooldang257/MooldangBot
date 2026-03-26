using System.ComponentModel.DataAnnotations;

namespace MooldangBot.Domain.Entities
{
    public class StreamerProfile
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
        public DateTime? TokenExpiresAt { get; set; }

        // [추가] 1. API 키 보관 (명세서: 보안 관리를 위해 DB에 저장)
        [MaxLength(200)]
        public string? ApiClientId { get; set; }

        [MaxLength(200)]
        public string? ApiClientSecret { get; set; }

        // [추가] 2. 공지 메모
        public string? NoticeMemo { get; set; }

        // [추가] 3. 물마카세(오마카세) 및 명령어 설정
        [ConcurrencyCheck]
        public int OmakaseCount { get; set; } = 0; // 현재 쌓인 물마카세 개수

        [MaxLength(50)]
        public string OmakaseCommand { get; set; } = "!물마카세"; // 오마카세 명령어

        public int OmakasePrice { get; set; } = 1000; // 기준 치즈 금액

        [MaxLength(50)]
        public string SongCommand { get; set; } = "!신청"; // 일반 신청 명령어

        public int SongPrice { get; set; } = 0; // 일반 신청 기준 금액 (0이면 무료)

        // [추가] 4. 화면 세부 디자인 설정 (복잡한 컴포넌트 설정은 JSON으로 통째로 저장)
        public string? DesignSettingsJson { get; set; }

        // [추가] 5. 시청자 포인트 및 출석 설정
        public int PointPerChat { get; set; } = 1;
        public int PointPerDonation1000 { get; set; } = 10;
        public int PointPerAttendance { get; set; } = 10;

        [MaxLength(200)]
        public string AttendanceCommands { get; set; } = "출석,물하,댕하";

        [MaxLength(200)]
        public string AttendanceReply { get; set; } = "{닉네임}님 출석 고마워요!";

        [MaxLength(200)]
        public string PointCheckCommand { get; set; } = "!내정보,!포인트";

        [MaxLength(200)]
        public string PointCheckReply { get; set; } = "🪙 {닉네임}님의 보유 포인트는 {포인트}점입니다! (누적 출석: {출석일수}일)";

        // [추가] 스트리머 전용 커스텀 봇 계정 토큰 정보
        public string? BotAccessToken { get; set; }
        public string? BotRefreshToken { get; set; }
        public DateTime? BotTokenExpiresAt { get; set; }

        // [추가] 물댕봇 세션 활성화/비활성화 상태
        public bool IsBotEnabled { get; set; } = false;

        public bool IsOmakaseEnabled { get; set; } = true;

        public int? ActiveOverlayPresetId { get; set; }
    }
}