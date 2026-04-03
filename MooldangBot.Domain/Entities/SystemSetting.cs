using System.ComponentModel.DataAnnotations;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    // 시스템 전체 공통 설정을 보관하는 마스터 테이블
    public class SystemSetting : ISoftDeletable, IAuditable
    {
        [Key]
        [MaxLength(100)]
        public string KeyName { get; set; } = string.Empty; // 예: "ChzzkClientId"

        public string KeyValue { get; set; } = string.Empty; // 실제 키 값


        // ⭐ 봇 회신용 토큰 저장소 추가
        public string? BotAccessToken { get; set; }
        public string? BotRefreshToken { get; set; }

        // [v6.2.2] 거버넌스 및 감사 필드
        public bool IsDeleted { get; set; } = false;
        public MooldangBot.Domain.Common.KstClock? DeletedAt { get; set; }
        public MooldangBot.Domain.Common.KstClock CreatedAt { get; set; } = MooldangBot.Domain.Common.KstClock.Now;
        public MooldangBot.Domain.Common.KstClock? UpdatedAt { get; set; }
    }
}